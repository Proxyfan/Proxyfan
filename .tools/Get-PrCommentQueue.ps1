#Requires -Version 7

<#
.SYNOPSIS
    Manages a per-pull-request comment work queue for Copilot coding agents.

.DESCRIPTION
    Pull-request review comments are an external source of truth: they live in
    GitHub. But agents need to walk them down systematically across many
    sessions without re-discovering already-resolved work or re-parsing the
    raw GitHub JSON each time.

    This script maintains a lightweight, per-PR JSON queue under
    `~/.copilot/pr-queues/pr-<owner>-<repo>-<number>.json` that tracks the
    *state* of each comment locally — `pending`, `in_progress`, or `resolved`
    — while always re-fetching the comment bodies from GitHub on `Refresh`.
    The queue is therefore safe to drop and rebuild at any time; the only
    durable state is the set of comment IDs already marked done.

    Severity is parsed from the comment body: agentic-workflow specialist
    output uses either bracket tags (`[MAX]`, `[HIGH]`, `[MEDIUM]`, `[LOW]`)
    or a bold/Markdown `**Severity:** <Critical|High|Medium|Low>` line.

    Exit codes:
        0 — Action completed successfully.
        1 — Unusable input, missing PR, missing required `gh` CLI, or the
            requested comment id does not exist.

.PARAMETER Pr
    Pull-request number (e.g. `42`) or full URL
    (e.g. `https://github.com/Proxyfan/Proxyfan/pull/42`).

.PARAMETER Action
    Operation to perform.
        Status   (default) Print the queue as a table.
        Next     Return the next N pending comments without changing state.
        Pop      Return the next N pending comments and mark them in_progress.
        Done     Mark a specific comment id as resolved.
        Refresh  Re-fetch comments from GitHub, merging local status.

.PARAMETER Count
    How many comments Next / Pop should return. Defaults to 1.

.PARAMETER Id
    Comment id (string). Required for Done.

.PARAMETER Severity
    Filter by one or more severities. Accepts MAX, HIGH, MEDIUM, LOW.

.PARAMETER Json
    For Status, emit a JSON document instead of a table. Next / Pop already
    emit JSON (their primary consumer is a coding agent).

.PARAMETER Help
    Show usage and exit.

.EXAMPLE
    .\.tools\Get-PrCommentQueue.ps1 -Pr 42
    Show all queued comments for PR #42.

.EXAMPLE
    .\.tools\Get-PrCommentQueue.ps1 -Pr 42 -Action Pop -Count 3 -Severity HIGH,MAX
    Take the three oldest unresolved HIGH/MAX comments and mark them in_progress.

.EXAMPLE
    .\.tools\Get-PrCommentQueue.ps1 -Pr 42 -Action Done -Id 1234567890
    Mark that comment as resolved.

.EXAMPLE
    .\.tools\Get-PrCommentQueue.ps1 -Pr 42 -Action Refresh
    Re-fetch comments from GitHub and merge into the local queue.

.NOTES
    Requires the `gh` CLI (GitHub authentication is delegated to it).
#>

[CmdletBinding()]
param(
    [string] $Pr,

    [ValidateSet('Status', 'Next', 'Pop', 'Done', 'Refresh')]
    [string] $Action = 'Status',

    [int] $Count = 1,

    [string] $Id,

    [ValidateSet('MAX', 'HIGH', 'MEDIUM', 'LOW')]
    [string[]] $Severity,

    [switch] $Json,

    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$ScriptDir/PowerShell/Modules/Output.psm1" -Force

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan PR Comment Queue' -ForegroundColor Cyan
    Write-Host '=========================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage:'
    Write-Host '  .\.tools\Get-PrCommentQueue.ps1 -Pr <number|url> [-Action <action>] [options]'
    Write-Host ''
    Write-Host 'Actions:'
    Write-Host '  Status   Print all queued comments (default).'
    Write-Host '  Next     Return the next N pending comments, do not change state.'
    Write-Host '  Pop      Return the next N pending comments, mark in_progress.'
    Write-Host '  Done     Mark a comment id as resolved.'
    Write-Host '  Refresh  Re-fetch comments from GitHub.'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Count <n>         How many comments Next/Pop should return (default 1)'
    Write-Host '  -Id <id>           Comment id (required for Done)'
    Write-Host '  -Severity <list>   Filter to MAX, HIGH, MEDIUM, LOW (comma-separated)'
    Write-Host '  -Json              Emit JSON for Status (Next/Pop are JSON by default)'
    Write-Host ''
    Write-Host 'State is persisted under ~/.copilot/pr-queues/.'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

if (-not $Pr) {
    Write-Failure "-Pr is required (PR number or full GitHub PR URL). Pass -Help for usage."
    exit 1
}

# ─── GitHub plumbing ──────────────────────────────────────────────────────────

function Test-GhAvailable {
    $null = Get-Command gh -ErrorAction SilentlyContinue
    return $?
}

function Resolve-Pr {
    param([string] $Identifier)

    if ($Identifier -match 'github\.com/([^/]+)/([^/]+)/pull/(\d+)') {
        return [pscustomobject]@{
            Owner  = $Matches[1]
            Repo   = $Matches[2]
            Number = [int]$Matches[3]
        }
    }
    if ($Identifier -match '^\d+$') {
        $repoView = & gh repo view --json owner,name 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $repoView) {
            throw "Could not infer repo from current directory; supply a full PR URL instead of just the number."
        }
        $obj = $repoView | ConvertFrom-Json
        return [pscustomobject]@{
            Owner  = $obj.owner.login
            Repo   = $obj.name
            Number = [int]$Identifier
        }
    }
    throw "Could not parse PR identifier: '$Identifier'. Supply a PR number or full GitHub PR URL."
}

function Get-CommentSeverity {
    param([string] $Body)

    if (-not $Body) { return $null }

    # Bracket-style tags first.
    if ($Body -match '\[MAX\]'    -or $Body -match '🔴') { return 'MAX' }
    if ($Body -match '\[HIGH\]'   -or $Body -match '🟠') { return 'HIGH' }
    if ($Body -match '\[MEDIUM\]' -or $Body -match '🟡') { return 'MEDIUM' }
    if ($Body -match '\[LOW\]'    -or $Body -match '🟢') { return 'LOW' }

    # Markdown table / bold style: `**Severity:** Critical`, `| Severity | High |`
    if ($Body -imatch '\bSeverity\b[\s:|*]+(?:Max|Critical)') { return 'MAX' }
    if ($Body -imatch '\bSeverity\b[\s:|*]+High')             { return 'HIGH' }
    if ($Body -imatch '\bSeverity\b[\s:|*]+Medium')           { return 'MEDIUM' }
    if ($Body -imatch '\bSeverity\b[\s:|*]+Low')              { return 'LOW' }

    return $null
}

function Fetch-Comments {
    param([pscustomobject] $PrInfo)

    $result = [System.Collections.Generic.List[pscustomobject]]::new()

    $review = & gh api "/repos/$($PrInfo.Owner)/$($PrInfo.Repo)/pulls/$($PrInfo.Number)/comments" --paginate 2>$null
    if ($LASTEXITCODE -eq 0 -and $review) {
        $review | ConvertFrom-Json | ForEach-Object {
            [void]$result.Add([pscustomobject]@{
                id         = $_.id.ToString()
                kind       = 'review'
                severity   = Get-CommentSeverity $_.body
                body       = $_.body
                file       = $_.path
                line       = $_.line
                author     = $_.user.login
                created_at = $_.created_at
                url        = $_.html_url
            })
        }
    }

    $issue = & gh api "/repos/$($PrInfo.Owner)/$($PrInfo.Repo)/issues/$($PrInfo.Number)/comments" --paginate 2>$null
    if ($LASTEXITCODE -eq 0 -and $issue) {
        $issue | ConvertFrom-Json | ForEach-Object {
            [void]$result.Add([pscustomobject]@{
                id         = $_.id.ToString()
                kind       = 'issue'
                severity   = Get-CommentSeverity $_.body
                body       = $_.body
                file       = $null
                line       = $null
                author     = $_.user.login
                created_at = $_.created_at
                url        = $_.html_url
            })
        }
    }

    return $result | Sort-Object created_at
}

# ─── Queue persistence ────────────────────────────────────────────────────────

function Get-QueuePath {
    param([pscustomobject] $PrInfo)

    $dir = Join-Path $HOME '.copilot' 'pr-queues'
    if (-not (Test-Path -LiteralPath $dir)) {
        $null = New-Item -ItemType Directory -Path $dir -Force
    }
    return Join-Path $dir ("pr-{0}-{1}-{2}.json" -f $PrInfo.Owner, $PrInfo.Repo, $PrInfo.Number)
}

function Read-Queue {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Write-Queue {
    param([string] $Path, [pscustomobject] $Queue)
    ($Queue | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Build-MergedQueue {
    param([pscustomobject] $PrInfo, [string] $Path)

    $fetched  = Fetch-Comments -PrInfo $PrInfo
    $existing = Read-Queue -Path $Path
    $statusMap = @{}
    if ($existing -and $existing.comments) {
        foreach ($c in $existing.comments) { $statusMap[$c.id] = $c.status }
    }

    $merged = $fetched | ForEach-Object {
        $status = if ($statusMap.ContainsKey($_.id)) { $statusMap[$_.id] } else { 'pending' }
        [pscustomobject]@{
            id         = $_.id
            kind       = $_.kind
            severity   = $_.severity
            body       = $_.body
            file       = $_.file
            line       = $_.line
            author     = $_.author
            created_at = $_.created_at
            url        = $_.url
            status     = $status
        }
    }

    return [pscustomobject]@{
        owner      = $PrInfo.Owner
        repo       = $PrInfo.Repo
        pr         = $PrInfo.Number
        schema     = 1
        fetched_at = (Get-Date).ToUniversalTime().ToString('o')
        comments   = @($merged)
    }
}

# ─── Action handlers ──────────────────────────────────────────────────────────

function Apply-SeverityFilter {
    param([object[]] $Comments)
    if (-not $Severity) { return $Comments }
    return @($Comments | Where-Object { $_.severity -in $Severity })
}

function Render-StatusTable {
    param([pscustomobject] $Queue)

    $all = Apply-SeverityFilter -Comments $Queue.comments
    $pending     = @($all | Where-Object status -eq 'pending')
    $inProgress  = @($all | Where-Object status -eq 'in_progress')
    $resolved    = @($all | Where-Object status -eq 'resolved')

    Write-Host ''
    Write-Host ("  PR #{0} — {1}/{2}" -f $Queue.pr, $Queue.owner, $Queue.repo) -ForegroundColor Cyan
    Write-Host ("  Fetched: {0}" -f $Queue.fetched_at)                          -ForegroundColor Gray
    Write-Host ''
    Write-Host ("  pending:     {0}" -f $pending.Count)    -ForegroundColor Yellow
    Write-Host ("  in_progress: {0}" -f $inProgress.Count) -ForegroundColor Cyan
    Write-Host ("  resolved:    {0}" -f $resolved.Count)   -ForegroundColor Green
    Write-Host ''

    if ($all.Count -eq 0) { return }

    Write-Host ('  {0,-14} {1,-8} {2,-12} {3,-22} {4}' -f 'ID', 'Severity', 'Status', 'File:Line', 'Preview') -ForegroundColor White
    Write-Host ('  ' + ('-' * 100)) -ForegroundColor Gray

    foreach ($c in ($all | Sort-Object created_at)) {
        $sevColor = switch ($c.severity) {
            'MAX'    { 'Red' }
            'HIGH'   { 'DarkYellow' }
            'MEDIUM' { 'Yellow' }
            'LOW'    { 'Green' }
            default  { 'Gray' }
        }
        $stColor = switch ($c.status) {
            'pending'     { 'Yellow' }
            'in_progress' { 'Cyan' }
            'resolved'    { 'DarkGray' }
            default       { 'Gray' }
        }
        $location = if ($c.file) { ("{0}:{1}" -f $c.file, ($c.line ?? '?')) } else { '-' }
        $body     = if ($c.body) { ($c.body -replace '\r?\n', ' ') } else { '' }
        $preview  = if ($body.Length -gt 60) { $body.Substring(0, 57) + '...' } else { $body }

        Write-Host ('  {0,-14}' -f $c.id) -NoNewline
        Write-Host ('{0,-8}'  -f ($c.severity ?? 'none')) -NoNewline -ForegroundColor $sevColor
        Write-Host ('{0,-12}' -f $c.status)               -NoNewline -ForegroundColor $stColor
        Write-Host ('{0,-22}' -f $location)               -NoNewline
        Write-Host $preview                                          -ForegroundColor Gray
    }
    Write-Host ''
}

function Select-Pending {
    param([pscustomobject] $Queue, [int] $N, [switch] $MarkInProgress)

    $candidates = Apply-SeverityFilter -Comments ($Queue.comments | Where-Object status -eq 'pending')
    $top = @($candidates | Sort-Object created_at | Select-Object -First $N)

    if ($MarkInProgress -and $top.Count -gt 0) {
        $ids = $top | Select-Object -ExpandProperty id
        foreach ($c in $Queue.comments) {
            if ($c.id -in $ids) { $c.status = 'in_progress' }
        }
    }
    return $top
}

# ─── Main ─────────────────────────────────────────────────────────────────────

if (-not (Test-GhAvailable)) {
    Write-Failure "'gh' CLI is not on PATH. Install it from https://cli.github.com/."
    exit 1
}

try {
    $prInfo  = Resolve-Pr -Identifier $Pr
} catch {
    Write-Failure $_.Exception.Message
    exit 1
}

$queuePath = Get-QueuePath -PrInfo $prInfo
$queue     = Read-Queue -Path $queuePath

if ($Action -eq 'Refresh' -or -not $queue) {
    Write-Step ("Fetching comments for PR #{0}..." -f $prInfo.Number)
    $queue = Build-MergedQueue -PrInfo $prInfo -Path $queuePath
    Write-Queue -Path $queuePath -Queue $queue
    Write-Success ("Queue saved ({0} comments)" -f $queue.comments.Count)
}

switch ($Action) {
    'Status' {
        if ($Json) {
            $filtered = Apply-SeverityFilter -Comments $queue.comments
            $out = [pscustomobject]@{
                owner      = $queue.owner
                repo       = $queue.repo
                pr         = $queue.pr
                fetched_at = $queue.fetched_at
                comments   = @($filtered)
            }
            ($out | ConvertTo-Json -Depth 12) | Write-Output
        } else {
            Render-StatusTable -Queue $queue
        }
    }
    'Next' {
        $picked = Select-Pending -Queue $queue -N $Count
        ($picked | ConvertTo-Json -Depth 12) | Write-Output
    }
    'Pop' {
        $picked = Select-Pending -Queue $queue -N $Count -MarkInProgress
        Write-Queue -Path $queuePath -Queue $queue
        ($picked | ConvertTo-Json -Depth 12) | Write-Output
    }
    'Done' {
        if (-not $Id) {
            Write-Failure '-Id is required for Done.'
            exit 1
        }
        $target = $queue.comments | Where-Object id -eq $Id
        if (-not $target) {
            Write-Failure "Comment id '$Id' is not in the queue (try -Action Refresh)."
            exit 1
        }
        $target.status = 'resolved'
        Write-Queue -Path $queuePath -Queue $queue
        Write-Success ("Comment {0} marked resolved." -f $Id)
        if (-not $Json) { Render-StatusTable -Queue $queue }
    }
    'Refresh' {
        if ($Json) {
            ($queue | ConvertTo-Json -Depth 12) | Write-Output
        } else {
            Render-StatusTable -Queue $queue
        }
    }
}

exit 0
