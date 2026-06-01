#Requires -Version 7

<#
.SYNOPSIS
    Extracts a backlog item — Epic / Feature / Use Case / Task — from
    docs/BACKLOG.md without forcing the agent to read the whole file.

.DESCRIPTION
    docs/BACKLOG.md is large (1800+ lines). Loading it end-to-end every time
    an agent needs the context for a single task wastes turns and pollutes
    context. This script accepts a backlog id prefix and prints just the
    matching block(s):

        E01                                Whole epic (every feature / UC / task).
        E01-F01                            Feature within an epic.
        E01-F01-UC01                       One use case (all its tasks).
        E01-F01-UC01-T01                   A single task.

    Backlog id format (see docs/BACKLOG.md § Task ID Format):
        E{NN}-F{NN}-UC{NN}-T{NN}

    The parser only relies on the heading shape, so it tolerates the
    surrounding prose, tables, and cross-references.

    Exit codes:
        0 — Matching block printed.
        1 — Backlog file missing, no matches, ambiguous prefix without
            -All, or invalid id format.

.PARAMETER Id
    Backlog id prefix. Required. Examples: `E01`, `E04-F02`, `E04-F02-UC01-T01`.

.PARAMETER All
    For an ambiguous prefix (e.g. `E01` matches an entire epic), print every
    matching block instead of failing. Default is to print whatever level the
    prefix names directly.

.PARAMETER List
    Instead of printing block bodies, list the matching headings only.
    Useful for "what tasks live under E04-F01-UC02?" style questions.

.PARAMETER Json
    Emit a structured JSON document (id, level, title, body lines).

.PARAMETER BacklogPath
    Path to the backlog. Defaults to docs/BACKLOG.md under the repo root.

.PARAMETER Help
    Show usage and exit.

.EXAMPLE
    .\.tools\Get-WorkItem.ps1 -Id E01-F01-UC01-T01
    Print the full text of task T01 inside UC01 of F01 of epic E01.

.EXAMPLE
    .\.tools\Get-WorkItem.ps1 -Id E04-F02 -List
    List every Use Case / Task heading under feature F02 of epic E04.

.EXAMPLE
    .\.tools\Get-WorkItem.ps1 -Id E10 -All -Json
    Emit JSON for every block under epic E10.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Id,

    [switch] $All,
    [switch] $List,
    [switch] $Json,
    [string] $BacklogPath,
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

Import-Module "$ScriptDir/PowerShell/Modules/Output.psm1" -Force

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Work-Item Extractor' -ForegroundColor Cyan
    Write-Host '============================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Get-WorkItem.ps1 -Id <id-prefix> [-All] [-List] [-Json] [-BacklogPath <path>]'
    Write-Host ''
    Write-Host 'Id forms (any prefix is accepted):'
    Write-Host '  E01                       Whole epic'
    Write-Host '  E01-F01                   Feature within an epic'
    Write-Host '  E01-F01-UC01              Use case (all tasks)'
    Write-Host '  E01-F01-UC01-T01          Single task'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

# Validate id shape.
$idPattern = '^E\d{1,2}(?:-F\d{1,2}(?:-UC\d{1,2}(?:-T\d{1,2})?)?)?$'
if ($Id -notmatch $idPattern) {
    Write-Failure "Invalid backlog id '$Id'. Expected E{NN}[-F{NN}[-UC{NN}[-T{NN}]]]."
    exit 1
}

if (-not $BacklogPath) {
    $BacklogPath = Join-Path $RepoRoot 'docs/BACKLOG.md'
}
if (-not (Test-Path -LiteralPath $BacklogPath)) {
    Write-Failure "Backlog file not found: $BacklogPath"
    exit 1
}

$Lines = Get-Content -LiteralPath $BacklogPath
$Total = $Lines.Count

# ─── Parse all blocks into a flat list with start/end line numbers ────────────

class BacklogBlock {
    [string]   $Id
    [string]   $Level    # epic | feature | use-case | task
    [string]   $Title
    [int]      $Start    # 1-based, inclusive
    [int]      $End      # 1-based, inclusive
}

function Parse-Blocks {
    param([string[]] $Lines)

    $blocks = [System.Collections.Generic.List[BacklogBlock]]::new()
    $currentEpic = $null
    $currentFeature = $null
    $currentUseCase = $null

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]

        # Epic:    "### E01 — Proxy Engine Core"
        if ($line -match '^###\s+(E\d{1,2})\s+[-—]\s+(.+?)\s*$') {
            $currentEpic    = $Matches[1]
            $currentFeature = $null
            $currentUseCase = $null
            $blocks.Add([BacklogBlock]@{
                Id    = $currentEpic
                Level = 'epic'
                Title = $Matches[2]
                Start = $i + 1
                End   = $Total
            })
            continue
        }

        # Feature: "#### F01 — TCP Listener and Connection Handling (Size: L)"
        if ($line -match '^####\s+(F\d{1,2})\s+[-—]\s+(.+?)\s*$') {
            if (-not $currentEpic) { continue }
            $currentFeature = "$currentEpic-$($Matches[1])"
            $currentUseCase = $null
            $blocks.Add([BacklogBlock]@{
                Id    = $currentFeature
                Level = 'feature'
                Title = $Matches[2]
                Start = $i + 1
                End   = $Total
            })
            continue
        }

        # Use case: "##### UC01 — Accept Incoming TCP Connections"
        if ($line -match '^#####\s+(UC\d{1,2})\s+[-—]\s+(.+?)\s*$') {
            if (-not $currentFeature) { continue }
            $currentUseCase = "$currentFeature-$($Matches[1])"
            $blocks.Add([BacklogBlock]@{
                Id    = $currentUseCase
                Level = 'use-case'
                Title = $Matches[2]
                Start = $i + 1
                End   = $Total
            })
            continue
        }

        # Task: "**T01 — Implement TCP listener with configurable port**"
        if ($line -match '^\*\*(T\d{1,2})\s+[-—]\s+(.+?)\*\*\s*$') {
            if (-not $currentUseCase) { continue }
            $taskId = "$currentUseCase-$($Matches[1])"
            $blocks.Add([BacklogBlock]@{
                Id    = $taskId
                Level = 'task'
                Title = $Matches[2]
                Start = $i + 1
                End   = $Total
            })
            continue
        }
    }

    # Close each block at the line before the next block of equal or higher level.
    $rank = @{ 'epic' = 1; 'feature' = 2; 'use-case' = 3; 'task' = 4 }
    for ($i = 0; $i -lt $blocks.Count; $i++) {
        $b = $blocks[$i]
        for ($j = $i + 1; $j -lt $blocks.Count; $j++) {
            if ($rank[$blocks[$j].Level] -le $rank[$b.Level]) {
                $b.End = $blocks[$j].Start - 1
                break
            }
        }
    }

    return $blocks
}

$Blocks = Parse-Blocks -Lines $Lines

# ─── Match blocks against the requested id ────────────────────────────────────

function Compare-Id {
    param([string] $Requested, [string] $Candidate)

    if ($Requested -eq $Candidate) { return 'exact' }
    if ($Candidate.StartsWith("$Requested-")) { return 'descendant' }
    return 'none'
}

$exact      = @($Blocks | Where-Object { (Compare-Id $Id $_.Id) -eq 'exact' })
$descendant = @($Blocks | Where-Object { (Compare-Id $Id $_.Id) -eq 'descendant' })

if ($exact.Count -eq 0 -and $descendant.Count -eq 0) {
    Write-Failure "No backlog block matches '$Id'."
    exit 1
}

# Determine the set we'll emit.
$matches = if ($List -or $All) {
    @($exact + $descendant)
} elseif ($exact.Count -gt 0) {
    $exact
} else {
    # Prefix without exact hit but with descendants — print the descendants.
    $descendant
}

# ─── Emit ─────────────────────────────────────────────────────────────────────

if ($List) {
    if ($Json) {
        $payload = @($matches | ForEach-Object {
            [pscustomobject]@{
                id    = $_.Id
                level = $_.Level
                title = $_.Title
                lines = @{ start = $_.Start; end = $_.End }
            }
        })
        ($payload | ConvertTo-Json -Depth 6) | Write-Output
    } else {
        Write-Banner "Backlog matches for '$Id'"
        foreach ($m in $matches) {
            Write-Info ("{0,-10} {1,-22} {2}" -f $m.Level, $m.Id, $m.Title)
        }
    }
    exit 0
}

if ($Json) {
    $payload = @($matches | ForEach-Object {
        $body = $Lines[($_.Start - 1)..($_.End - 1)] -join "`n"
        [pscustomobject]@{
            id    = $_.Id
            level = $_.Level
            title = $_.Title
            lines = @{ start = $_.Start; end = $_.End }
            body  = $body
        }
    })
    ($payload | ConvertTo-Json -Depth 6) | Write-Output
    exit 0
}

foreach ($m in $matches) {
    Write-Banner ("{0} — {1} ({2})" -f $m.Id, $m.Title, $m.Level)
    $slice = $Lines[($m.Start - 1)..($m.End - 1)]
    $slice | Out-Host
}

exit 0
