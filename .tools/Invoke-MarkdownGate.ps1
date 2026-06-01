#Requires -Version 7

<#
.SYNOPSIS
    Markdown freshness and size gate for Proxyfan's agent-facing docs.

.DESCRIPTION
    Coding-agent context windows are finite. The `.github/copilot-instructions.md`
    file, every `.github/instructions/*.instructions.md`, and every
    `.github/skills/*/SKILL.md` file is loaded into context per-turn whenever
    its `applyTo:` matches the file under edit. If any of those files balloons
    in size, every future agent turn pays the cost.

    This gate enforces two policies:

      1. **Size limits per category.** Each agent-loaded file category has a
         conservative line + character cap (see `Get-CategoryLimit`). Other
         repo markdown (README, docs/, ARCHITECTURE, BACKLOG, JOURNAL …) has
         a looser cap that flags single docs that have grown beyond the
         single-purpose threshold and should be split.

      2. **Freshness window for agent-loaded docs.** If an instruction file
         or SKILL.md has not been touched (by any commit) within
         `FreshnessDays`, the gate fails. The intent is to surface stale
         agent guidance that has drifted from the actual codebase, not to
         force meaningless edits.

    The gate is **opt-in** — `.tools/Invoke-Build.ps1` does NOT run it by
    default. Run it manually before merging documentation changes, or pass
    `-CheckMarkdown` to the build script.

    Exit codes:
        0 — All checks passed.
        1 — At least one file violated a size or freshness rule.
        2 — Invalid invocation (e.g. ChangedOnly without git).

.PARAMETER FreshnessDays
    Maximum age (in days, by last commit) for agent-loaded docs. Defaults to
    60, which is generous; a stale instruction file is allowed to coast as
    long as the codebase it describes is stable, but it must be reviewed at
    least every two months.

.PARAMETER SkipFreshness
    Skip the freshness check. The size check still runs.

.PARAMETER ChangedOnly
    Only check markdown files touched by the current working tree (staged +
    unstaged + untracked). Use this for the pre-commit gate so a stale doc
    on `main` does not block an unrelated PR.

.PARAMETER Json
    Emit a JSON summary instead of human-readable text.

.PARAMETER Help
    Show usage and exit.

.EXAMPLE
    .\.tools\Invoke-MarkdownGate.ps1
    Check every markdown file in the repository.

.EXAMPLE
    .\.tools\Invoke-MarkdownGate.ps1 -ChangedOnly
    Only check markdown files in the current branch's working tree.

.EXAMPLE
    .\.tools\Invoke-MarkdownGate.ps1 -SkipFreshness
    Size-limits only; useful in CI where commit dates carry no meaning.
#>

[CmdletBinding()]
param(
    [int]    $FreshnessDays = 60,
    [switch] $SkipFreshness,
    [switch] $ChangedOnly,
    [switch] $Json,
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

Import-Module "$ScriptDir/PowerShell/Modules/Output.psm1" -Force

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Markdown Gate' -ForegroundColor Cyan
    Write-Host '======================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Invoke-MarkdownGate.ps1 [options]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -FreshnessDays <n>   Max age (days) for agent-loaded docs (default: 60)'
    Write-Host '  -SkipFreshness       Run size limits only'
    Write-Host '  -ChangedOnly         Only check files in the current working tree'
    Write-Host '  -Json                JSON summary instead of human text'
    Write-Host '  -Help                Show this help'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

# ─── Category map ────────────────────────────────────────────────────────────

function Get-CategoryLimit {
    param([string] $RelPath)

    $p = $RelPath.Replace('\', '/')

    if ($p -eq '.github/copilot-instructions.md') {
        return [pscustomobject]@{ Category = 'copilot-instructions'; MaxLines = 260; MaxChars = 10000; AgentLoaded = $true }
    }
    if ($p -match '^\.github/instructions/.+\.instructions\.md$') {
        return [pscustomobject]@{ Category = 'instruction';          MaxLines = 250; MaxChars = 10000; AgentLoaded = $true }
    }
    if ($p -match '^\.github/skills/.+/SKILL\.md$') {
        return [pscustomobject]@{ Category = 'skill';                MaxLines = 200; MaxChars = 8000; AgentLoaded = $true }
    }
    if ($p -match '^\.github/skills/.+\.md$') {
        return [pscustomobject]@{ Category = 'skill-companion';      MaxLines = 400; MaxChars = 12000; AgentLoaded = $false }
    }
    if ($p -eq '.github/journal-protocol.md') {
        return [pscustomobject]@{ Category = 'journal-protocol';     MaxLines = 200; MaxChars = 6000; AgentLoaded = $true }
    }
    if ($p -eq 'JOURNAL.md') {
        return [pscustomobject]@{ Category = 'journal';              MaxLines = 800; MaxChars = 40000; AgentLoaded = $false }
    }
    if ($p -eq 'AGENTS.md' -or $p -eq 'CONTRIBUTING.md' -or $p -eq 'README.md') {
        return [pscustomobject]@{ Category = 'top-level';            MaxLines = 600; MaxChars = 20000; AgentLoaded = $false }
    }
    if ($p -match '^docs/') {
        # docs/BACKLOG.md, docs/ARCHITECTURE.md, and docs/DESIGN.md are
        # intentionally large reference documents — they index work, system
        # structure, and behavioural contracts respectively, and are not
        # auto-loaded into the agent context. Gate them loosely.
        if ($p -in @('docs/BACKLOG.md', 'docs/ARCHITECTURE.md', 'docs/DESIGN.md')) {
            return [pscustomobject]@{ Category = 'reference';        MaxLines = 5000; MaxChars = 250000; AgentLoaded = $false }
        }
        return [pscustomobject]@{ Category = 'docs';                MaxLines = 1200; MaxChars = 40000; AgentLoaded = $false }
    }
    return [pscustomobject]@{ Category = 'other';                    MaxLines = 600; MaxChars = 20000; AgentLoaded = $false }
}

# ─── Discovery ───────────────────────────────────────────────────────────────

function Get-AllMarkdownFiles {
    $excludePatterns = @(
        '\\node_modules\\', '\\bin\\', '\\obj\\', '\\.git\\',
        '\\packages\\', '\\publish\\', '\\artifacts\\'
    )
    Get-ChildItem -Path $RepoRoot -Recurse -Filter '*.md' -File -ErrorAction SilentlyContinue |
        Where-Object {
            $full = $_.FullName
            -not ($excludePatterns | Where-Object { $full -match $_ })
        } |
        Select-Object -ExpandProperty FullName
}

function Get-ChangedMarkdownFiles {
    $staged    = & git diff --name-only --cached    2>$null
    $unstaged  = & git diff --name-only             2>$null
    $untracked = & git ls-files --others --exclude-standard 2>$null

    $set = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($p in @($staged + $unstaged + $untracked)) {
        if ($p -and $p.ToLowerInvariant().EndsWith('.md')) {
            $abs = Join-Path $RepoRoot $p
            if (Test-Path -LiteralPath $abs) { [void]$set.Add($abs) }
        }
    }
    return @($set)
}

# ─── Main ─────────────────────────────────────────────────────────────────────

Push-Location $RepoRoot
try {
    if ($ChangedOnly) {
        $null = Get-Command git -ErrorAction SilentlyContinue
        if (-not $?) {
            Write-Failure "'git' is not available; -ChangedOnly cannot be used."
            exit 2
        }
        $files = Get-ChangedMarkdownFiles
    } else {
        $files = Get-AllMarkdownFiles
    }

    if (-not $Json) {
        Write-Banner ('Proxyfan Markdown Gate ({0} file{1})' -f $files.Count, ($(if ($files.Count -eq 1) { '' } else { 's' })))
    }

    $rootPrefix = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\','/') + [System.IO.Path]::DirectorySeparatorChar
    $now        = [System.DateTime]::UtcNow

    $sizeViolations      = [System.Collections.Generic.List[pscustomobject]]::new()
    $freshnessViolations = [System.Collections.Generic.List[pscustomobject]]::new()
    $rows                = [System.Collections.Generic.List[pscustomobject]]::new()

    foreach ($file in $files) {
        $rel    = $file.Substring($rootPrefix.Length).Replace('\', '/')
        $limit  = Get-CategoryLimit -RelPath $rel
        $raw    = Get-Content -LiteralPath $file -Raw -ErrorAction SilentlyContinue
        if ($null -eq $raw) { $raw = '' }
        $chars  = $raw.Length
        $lines  = @(Get-Content -LiteralPath $file -ErrorAction SilentlyContinue).Count

        $linesOver = $lines -gt $limit.MaxLines
        $charsOver = $chars -gt $limit.MaxChars
        $rowOver   = $linesOver -or $charsOver

        $ageDays   = $null
        $freshOver = $false
        if ($limit.AgentLoaded -and -not $SkipFreshness) {
            $gitDate = & git log -1 --format='%cI' -- $rel 2>$null
            if ($gitDate) {
                $commitTime = [System.DateTimeOffset]::Parse($gitDate).UtcDateTime
                $ageDays    = [int]([Math]::Round(($now - $commitTime).TotalDays))
                if ($ageDays -gt $FreshnessDays) { $freshOver = $true }
            }
        }

        $row = [pscustomobject]@{
            path         = $rel
            category     = $limit.Category
            agent_loaded = $limit.AgentLoaded
            lines        = $lines
            max_lines    = $limit.MaxLines
            chars        = $chars
            max_chars    = $limit.MaxChars
            age_days     = $ageDays
            size_ok      = -not $rowOver
            freshness_ok = -not $freshOver
        }
        [void]$rows.Add($row)

        if ($rowOver) {
            [void]$sizeViolations.Add([pscustomobject]@{
                path     = $rel
                category = $limit.Category
                lines    = $lines
                maxLines = $limit.MaxLines
                chars    = $chars
                maxChars = $limit.MaxChars
            })
        }
        if ($freshOver) {
            [void]$freshnessViolations.Add([pscustomobject]@{
                path     = $rel
                category = $limit.Category
                ageDays  = $ageDays
                maxDays  = $FreshnessDays
            })
        }
    }

    if ($Json) {
        $payload = [pscustomobject]@{
            checked_files        = $files.Count
            freshness_days       = $FreshnessDays
            skip_freshness       = [bool]$SkipFreshness
            changed_only         = [bool]$ChangedOnly
            size_violations      = @($sizeViolations)
            freshness_violations = @($freshnessViolations)
            rows                 = @($rows)
        }
        ($payload | ConvertTo-Json -Depth 6) | Write-Output
        if ($sizeViolations.Count -gt 0 -or $freshnessViolations.Count -gt 0) { exit 1 }
        exit 0
    }

    if ($sizeViolations.Count -eq 0) {
        Write-Success ("Size limits OK across {0} file(s)." -f $files.Count)
    } else {
        Write-Failure ("{0} file(s) exceed their size budget:" -f $sizeViolations.Count)
        foreach ($v in $sizeViolations) {
            $msg = "{0}  [{1}]" -f $v.path, $v.category
            if ($v.lines -gt $v.maxLines) {
                $msg += "  lines={0}/{1} (+{2})" -f $v.lines, $v.maxLines, ($v.lines - $v.maxLines)
            }
            if ($v.chars -gt $v.maxChars) {
                $msg += "  chars={0}/{1} (+{2})" -f $v.chars, $v.maxChars, ($v.chars - $v.maxChars)
            }
            Write-Info $msg
        }
    }

    if (-not $SkipFreshness) {
        if ($freshnessViolations.Count -eq 0) {
            Write-Success ("Freshness OK (window: {0} days)." -f $FreshnessDays)
        } else {
            Write-Failure ("{0} agent-loaded file(s) are stale (window: {1} days):" -f $freshnessViolations.Count, $FreshnessDays)
            foreach ($v in $freshnessViolations) {
                Write-Info ("{0}  [{1}]  age={2}d (limit {3}d)" -f $v.path, $v.category, $v.ageDays, $v.maxDays)
            }
        }
    }

    if ($sizeViolations.Count -gt 0 -or $freshnessViolations.Count -gt 0) {
        exit 1
    }

    Write-Banner 'Markdown gate passed'
    exit 0
}
finally {
    Pop-Location
}
