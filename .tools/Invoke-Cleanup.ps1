<#
.SYNOPSIS
    Runs JetBrains code cleanup on Proxyfan C# files, scoped to a deliberate set
    rather than the whole solution.

.DESCRIPTION
    Wraps `jb cleanupcode` (resolved either from PATH or restored as a local
    .NET tool) against the canonical Proxyfan profile. By default it cleans the
    .cs files in the current working tree (staged + unstaged + untracked). Pass
    explicit `-Path` arguments to clean an arbitrary subset, or `-ChangedSince`
    to scope to a specific git ref.

    The script never runs against the full solution implicitly — that is slow
    and rewrites files outside the agent's scope. If no files match, the script
    exits cleanly with no work done.

    Exit codes:
        0 — Cleanup completed (zero or more files processed).
        1 — Cleanup tool returned a non-zero status, or the working tree could
            not be inspected.
        2 — Cleanup tool was not found.

.PARAMETER Path
    One or more files or directories to clean. Directories are recursively
    expanded to *.cs files. When set, this OVERRIDES the working-tree scan.

.PARAMETER ChangedSince
    Limit cleanup to .cs files changed since this git ref (e.g. `origin/main`,
    `HEAD~3`). Combine with `-IncludeUnstaged` to also include uncommitted
    work; otherwise only committed changes are considered.

.PARAMETER IncludeUnstaged
    When `-ChangedSince` is set, ALSO include staged + unstaged + untracked
    files. Default: only diff against the ref.

.PARAMETER CheckOnly
    Run the cleanup with `--no-build` and detect whether files would change,
    but do not write the changes. Implemented by snapshotting file hashes
    before/after and rolling back any modifications. Exit non-zero on
    detected drift; useful in a pre-commit hook.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    .\.tools\Invoke-Cleanup.ps1
    Clean every .cs file in the current working tree.

.EXAMPLE
    .\.tools\Invoke-Cleanup.ps1 -Path src/Domain.Proxy
    Clean every .cs file under the Domain.Proxy module.

.EXAMPLE
    .\.tools\Invoke-Cleanup.ps1 -ChangedSince origin/main -IncludeUnstaged
    Clean every .cs file the current branch has touched, plus the local diff.

.EXAMPLE
    .\.tools\Invoke-Cleanup.ps1 -CheckOnly
    Report whether cleanup would change any file in the working tree; do not write.
#>

[CmdletBinding()]
param(
    [string[]] $Path,
    [string]   $ChangedSince,
    [switch]   $IncludeUnstaged,
    [switch]   $CheckOnly,
    [switch]   $Help
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

$OutputModule = Join-Path $ScriptDir 'PowerShell/Modules/Output.psm1'
if (Test-Path -LiteralPath $OutputModule) {
    Import-Module $OutputModule -Force
} else {
    function Write-Step    { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
    function Write-Success { param($Message) Write-Host '  [OK] '   -NoNewline -ForegroundColor Green;  Write-Host $Message }
    function Write-Failure { param($Message) Write-Host '  [FAIL] ' -NoNewline -ForegroundColor Red;    Write-Host $Message }
    function Write-Warn    { param($Message) Write-Host '  [WARN] ' -NoNewline -ForegroundColor Yellow; Write-Host $Message }
    function Write-Info    { param($Message) Write-Host "  $Message" -ForegroundColor Gray }
    function Write-Banner  { param($Title) Write-Host ''; Write-Host '========================================' -ForegroundColor Cyan; Write-Host "  $Title" -ForegroundColor Cyan; Write-Host '========================================' -ForegroundColor Cyan }
}

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Code Cleanup' -ForegroundColor Cyan
    Write-Host '=====================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Invoke-Cleanup.ps1 [-Path <list>] [-ChangedSince <ref>] [-IncludeUnstaged] [-CheckOnly]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Path <list>         Explicit files / directories (overrides working-tree scan)'
    Write-Host '  -ChangedSince <ref>  Diff against this git ref to pick files'
    Write-Host '  -IncludeUnstaged     Combine -ChangedSince with the local working tree'
    Write-Host '  -CheckOnly           Detect drift but do not write (rolls back any changes)'
    Write-Host '  -Help                Show this help'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\.tools\Invoke-Cleanup.ps1                              # Working-tree .cs files'
    Write-Host '  .\.tools\Invoke-Cleanup.ps1 -Path src/Domain.Proxy       # One module'
    Write-Host '  .\.tools\Invoke-Cleanup.ps1 -ChangedSince origin/main    # Branch diff'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

# ─── Cleanup tool resolution ──────────────────────────────────────────────────

function Resolve-CleanupCommand {
    $jb = Get-Command jb -ErrorAction SilentlyContinue
    if ($jb) {
        return ,@('jb', 'cleanupcode')
    }
    $cli = Get-Command jb.exe -ErrorAction SilentlyContinue
    if ($cli) {
        return ,@($cli.Source, 'cleanupcode')
    }
    # Try the local-tool form: `dotnet tool restore` + `dotnet jb cleanupcode`.
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        $check = & dotnet jb --help 2>$null
        if ($LASTEXITCODE -eq 0 -and $check) {
            return ,@('dotnet', 'jb', 'cleanupcode')
        }
    }
    return $null
}

# ─── File-set discovery ───────────────────────────────────────────────────────

function Expand-PathArguments {
    param([string[]] $Inputs)

    $out = [System.Collections.Generic.List[string]]::new()
    foreach ($p in $Inputs) {
        if (-not (Test-Path -LiteralPath $p)) {
            Write-Warn "Skipping missing path: $p"
            continue
        }
        $item = Get-Item -LiteralPath $p
        if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $p -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue |
                ForEach-Object { [void]$out.Add($_.FullName) }
        } elseif ($item.Extension -ieq '.cs') {
            [void]$out.Add($item.FullName)
        }
    }
    return @($out | Sort-Object -Unique)
}

function Get-WorkingTreeCsFiles {
    $set = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($args in @(
        @('diff', '--name-only', '--cached'),
        @('diff', '--name-only'),
        @('ls-files', '--others', '--exclude-standard')
    )) {
        $names = & git @args 2>$null
        foreach ($n in @($names)) {
            if ($n -and $n.ToLowerInvariant().EndsWith('.cs')) {
                $abs = Join-Path $RepoRoot $n
                if (Test-Path -LiteralPath $abs) { [void]$set.Add((Resolve-Path -LiteralPath $abs).Path) }
            }
        }
    }
    return @($set)
}

function Get-ChangedSinceCsFiles {
    param([string] $Ref)

    $null = & git rev-parse --verify --quiet $Ref 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "git ref not found: $Ref"
    }

    $names = & git diff --name-only "$Ref...HEAD" 2>$null
    $set   = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($n in @($names)) {
        if ($n -and $n.ToLowerInvariant().EndsWith('.cs')) {
            $abs = Join-Path $RepoRoot $n
            if (Test-Path -LiteralPath $abs) { [void]$set.Add((Resolve-Path -LiteralPath $abs).Path) }
        }
    }
    return @($set)
}

# ─── Main ─────────────────────────────────────────────────────────────────────

Push-Location $RepoRoot
try {
    Write-Banner 'Proxyfan Code Cleanup'

    $cleanup = Resolve-CleanupCommand
    if (-not $cleanup) {
        Write-Failure "JetBrains 'jb' cleanup tool not found on PATH (and 'dotnet jb' is not available)."
        Write-Info  'Run `dotnet tool restore` to install the local copy, or install the global tool with `dotnet tool install -g JetBrains.ReSharper.GlobalTools`.'
        exit 2
    }

    Write-Step 'Resolving target files...'
    $files = @()

    if ($Path -and $Path.Count -gt 0) {
        $files = Expand-PathArguments -Inputs $Path
        Write-Info "Explicit -Path scope: $($files.Count) file(s)."
    } elseif ($ChangedSince) {
        $files = Get-ChangedSinceCsFiles -Ref $ChangedSince
        Write-Info "Diff against ${ChangedSince}: $($files.Count) file(s)."
        if ($IncludeUnstaged) {
            $extra = Get-WorkingTreeCsFiles
            $files = @($files + $extra | Sort-Object -Unique)
            Write-Info "Plus working-tree changes: $($files.Count) file(s) total."
        }
    } else {
        $files = Get-WorkingTreeCsFiles
        Write-Info "Working-tree scope: $($files.Count) file(s)."
    }

    if ($files.Count -eq 0) {
        Write-Success 'No .cs files matched; nothing to clean up.'
        exit 0
    }

    # CheckOnly: snapshot SHA-256 hashes so we can roll back any rewrites.
    $beforeHashes = @{}
    if ($CheckOnly) {
        foreach ($f in $files) {
            $beforeHashes[$f] = (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash
        }
    }

    $includeArg = '--include=' + ($files -join ';')
    Write-Step 'Running JetBrains cleanup...'
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    & $cleanup[0] @($cleanup[1..($cleanup.Length - 1)] + @(
        '.\Proxyfan.slnx',
        '--profile=Custom: Full Cleanup',
        '--verbosity=WARN',
        '--no-build',
        $includeArg
    )) | ForEach-Object {
        Write-Host ("  [{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $_) -ForegroundColor Gray
    }
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($CheckOnly) {
        $changed = [System.Collections.Generic.List[string]]::new()
        foreach ($f in $files) {
            $after = (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash
            if ($after -ne $beforeHashes[$f]) {
                [void]$changed.Add($f)
                & git checkout -- $f 2>$null  # roll back if file is tracked
            }
        }
        if ($changed.Count -gt 0) {
            Write-Failure ("{0} file(s) would be modified by cleanup:" -f $changed.Count)
            foreach ($c in $changed) {
                Write-Info ("  " + $c.Substring($RepoRoot.Length).TrimStart('\','/'))
            }
            exit 1
        }
        Write-Success ("Cleanup is a no-op for the selected files ($Elapsed)")
        exit 0
    }

    if ($ExitCode -eq 0) {
        Write-Success ("Cleanup completed successfully ($Elapsed)")
        exit 0
    }

    Write-Failure ("Cleanup failed ($Elapsed)")
    exit 1
}
finally {
    Pop-Location
}
