#Requires -Version 7

<#
.SYNOPSIS
    Reports the current git working state of the Proxyfan repository in a
    deterministic, agent-friendly shape.

.DESCRIPTION
    Coding agents repeatedly need the same three pieces of context:

        1. What branch am I on and what is the base ref?
        2. Which files have I staged / modified / untracked?
        3. Which source / test / docs files were touched, and what
           validation commands should I run as a result?

    This script answers all three from a single `git` invocation set. Output
    is either a human-readable summary (default) or a JSON document
    (`-Json`) suitable for piping into a follow-up tool. The script never
    mutates the working tree.

    Exit codes:
        0 — Clean read.
        1 — Not a git repository, or git is not on PATH.

.PARAMETER BaseRef
    Git ref to diff against. Defaults to `origin/main` when present,
    otherwise `main`, otherwise `HEAD~1`.

.PARAMETER Json
    Emit a single JSON document with the same data as the text output.

.PARAMETER Quiet
    Suppress the human banner; print only the summary section. Ignored when
    `-Json` is supplied.

.PARAMETER Help
    Show usage and exit.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-RepoStatus.ps1
    Print a human-readable working-tree summary against the default base ref.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-RepoStatus.ps1 -Json
    Emit a JSON document for downstream tooling.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-RepoStatus.ps1 -BaseRef origin/main
    Pin the base ref explicitly.
#>

[CmdletBinding()]
param(
    [string] $BaseRef,
    [switch] $Json,
    [switch] $Quiet,
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot/PowerShell/Modules/Output.psm1" -Force

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Repository Status' -ForegroundColor Cyan
    Write-Host '==========================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Get-RepoStatus.ps1 [-BaseRef <ref>] [-Json] [-Quiet] [-Help]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -BaseRef   Base git ref to diff against (default: origin/main, main, HEAD~1)'
    Write-Host '  -Json      Emit a single JSON document instead of human-readable text'
    Write-Host '  -Quiet     Drop the banner; keep only the summary'
    Write-Host '  -Help      Show this help'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

function Test-GitAvailable {
    $null = Get-Command git -ErrorAction SilentlyContinue
    return $?
}

function Invoke-Git {
    param([string[]] $GitArgs)
    $stdout = & git @GitArgs 2>$null
    return ,@($stdout)
}

function Resolve-BaseRef {
    if ($BaseRef) { return $BaseRef }

    foreach ($candidate in @('origin/main', 'main')) {
        $null = & git rev-parse --verify --quiet $candidate 2>$null
        if ($LASTEXITCODE -eq 0) { return $candidate }
    }
    return 'HEAD~1'
}

function Get-ChangedFiles {
    param([string] $Resolved)

    $staged    = Invoke-Git @('diff', '--name-only', '--cached')
    $unstaged  = Invoke-Git @('diff', '--name-only')
    $untracked = Invoke-Git @('ls-files', '--others', '--exclude-standard')
    $vs_base   = Invoke-Git @('diff', '--name-only', "$Resolved...HEAD")

    return [pscustomobject]@{
        Staged       = @($staged    | Where-Object { $_ })
        Unstaged     = @($unstaged  | Where-Object { $_ })
        Untracked    = @($untracked | Where-Object { $_ })
        VersusBase   = @($vs_base   | Where-Object { $_ })
    }
}

function Classify-File {
    param([string] $Path)

    $p = $Path.Replace('\', '/')

    if ($p -like 'src/*.cs'   -or $p -like 'src/*/*.cs')   { return 'src-cs' }
    if ($p -like 'tests/*'    -and $p -like '*.cs')         { return 'test-cs' }
    if ($p -like '*.csproj')                                { return 'csproj' }
    if ($p -like 'Directory.*.props' -or $p -like 'Directory.*.targets') { return 'msbuild-shared' }
    if ($p -like '*.axaml' -or $p -like '*.axaml.cs')      { return 'avalonia' }
    if ($p -like '*.resx')                                  { return 'resx' }
    if ($p -like 'docs/*' -or $p -eq 'README.md' -or $p -eq 'AGENTS.md' -or $p -eq 'CONTRIBUTING.md') { return 'docs' }
    if ($p -like '.github/instructions/*' -or $p -like '.github/skills/*' -or $p -eq '.github/copilot-instructions.md' -or $p -eq '.github/journal-protocol.md' -or $p -eq 'JOURNAL.md') { return 'agent-docs' }
    if ($p -like '.github/workflows/*')                     { return 'workflows' }
    if ($p -like '.tools/*')                                { return 'tools' }
    if ($p -like 'installer/*')                             { return 'installer' }
    if ($p -eq 'Proxyfan.slnx')                             { return 'solution' }
    return 'other'
}

function Get-SuggestedCommands {
    param([string[]] $Categories)

    $suggestions = [System.Collections.Generic.List[string]]::new()

    if ($Categories -contains 'src-cs' -or $Categories -contains 'test-cs' -or $Categories -contains 'csproj' -or $Categories -contains 'msbuild-shared' -or $Categories -contains 'avalonia' -or $Categories -contains 'solution') {
        [void]$suggestions.Add('pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore -RunTests')
    }
    if ($Categories -contains 'resx') {
        [void]$suggestions.Add('pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Test-ResourceKeys.ps1')
    }
    if ($Categories -contains 'agent-docs' -or $Categories -contains 'docs') {
        [void]$suggestions.Add('pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-MarkdownGate.ps1')
    }
    if ($Categories -contains 'tools') {
        [void]$suggestions.Add('pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1')
    }
    if ($suggestions.Count -eq 0) {
        [void]$suggestions.Add('# (no validation commands suggested for this change set)')
    }
    return $suggestions.ToArray()
}

# ─── Main ─────────────────────────────────────────────────────────────────────

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

if (-not (Test-GitAvailable)) {
    Write-Failure "'git' is not available on PATH."
    exit 1
}

Push-Location $RepoRoot
try {
    $null = & git rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "$RepoRoot is not inside a git working tree."
        exit 1
    }

    $branch   = (Invoke-Git @('rev-parse', '--abbrev-ref', 'HEAD'))[0]
    $headSha  = (Invoke-Git @('rev-parse', '--short', 'HEAD'))[0]
    $resolved = Resolve-BaseRef
    $files    = Get-ChangedFiles -Resolved $resolved

    $allChanged = @($files.Staged + $files.Unstaged + $files.Untracked + $files.VersusBase | Sort-Object -Unique)
    $categorised = @{}
    foreach ($f in $allChanged) {
        $c = Classify-File -Path $f
        if (-not $categorised.ContainsKey($c)) { $categorised[$c] = [System.Collections.Generic.List[string]]::new() }
        [void]$categorised[$c].Add($f)
    }

    $suggestions = Get-SuggestedCommands -Categories @($categorised.Keys)

    if ($Json) {
        $payload = [ordered]@{
            branch        = $branch
            head          = $headSha
            base_ref      = $resolved
            staged        = $files.Staged
            unstaged      = $files.Unstaged
            untracked     = $files.Untracked
            versus_base   = $files.VersusBase
            categories    = $categorised | ForEach-Object { @{ } } | Select-Object -First 1
            suggestions   = $suggestions
        }
        # Rebuild categories as a proper hashtable (the pipeline trick above is just placeholder syntax)
        $catOut = [ordered]@{}
        foreach ($k in ($categorised.Keys | Sort-Object)) { $catOut[$k] = @($categorised[$k]) }
        $payload.categories = $catOut

        ($payload | ConvertTo-Json -Depth 6) | Write-Output
        exit 0
    }

    if (-not $Quiet) {
        Write-Banner 'Proxyfan Repo Status'
    }

    Write-Step "Branch: $branch  @  $headSha   (base: $resolved)"

    if ($allChanged.Count -eq 0) {
        Write-Success 'Working tree and branch are clean against the base ref.'
        exit 0
    }

    if ($files.Staged.Count    -gt 0) { Write-Info ("Staged    ({0}): {1}" -f $files.Staged.Count,    ($files.Staged    -join ', ')) }
    if ($files.Unstaged.Count  -gt 0) { Write-Info ("Unstaged  ({0}): {1}" -f $files.Unstaged.Count,  ($files.Unstaged  -join ', ')) }
    if ($files.Untracked.Count -gt 0) { Write-Info ("Untracked ({0}): {1}" -f $files.Untracked.Count, ($files.Untracked -join ', ')) }
    if ($files.VersusBase.Count -gt 0) {
        Write-Info ("vs $resolved ({0}): {1}" -f $files.VersusBase.Count, ($files.VersusBase -join ', '))
    }

    Write-Host ''
    Write-Step 'Categories:'
    foreach ($k in ($categorised.Keys | Sort-Object)) {
        Write-Info ("{0,-16} {1}" -f $k, ($categorised[$k] -join ', '))
    }

    Write-Host ''
    Write-Step 'Suggested validation commands:'
    foreach ($s in $suggestions) { Write-Info $s }

    exit 0
}
finally {
    Pop-Location
}
