<#
.SYNOPSIS
    Runs JetBrains code cleanup on .cs files changed relative to HEAD.

.DESCRIPTION
    Finds all .cs files modified since HEAD and passes them to 'jb cleanupcode'
    using the 'Custom: Full Cleanup' profile. Only changed files are processed,
    keeping cleanup fast during incremental development.

    When no .cs files have changed, the script exits cleanly with no action.
    Cleanup output is timestamped so individual lines can be correlated in time.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    .\.tools\Invoke-Cleanup.ps1
    Clean up all .cs files modified since HEAD.
#>

[CmdletBinding()]
param(
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

# ─── Formatting helpers ────────────────────────────────────────────────────────

function Write-Step    { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host '  [OK] '   -NoNewline -ForegroundColor Green;  Write-Host $Message }
function Write-Info    { param($Message) Write-Host "  $Message" -ForegroundColor Gray }

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Code Cleanup' -ForegroundColor Cyan
    Write-Host '=====================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Invoke-Cleanup.ps1 [options]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Help    Show this help message'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\.tools\Invoke-Cleanup.ps1    # Clean up changed .cs files'
    Write-Host ''
}

# ─── Main ─────────────────────────────────────────────────────────────────────

if ($Help) {
    Show-Help
    exit 0
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

Push-Location $RepoRoot

try {
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host '  Proxyfan Code Cleanup' -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan

    Write-Step 'Collecting changed .cs files...'

    $Files = git diff HEAD --name-only | Where-Object { $_ -match '\.cs$' }

    if (-not $Files) {
        Write-Success 'No changed .cs files to clean up.'
        Write-Host ''
        Write-Host '========================================' -ForegroundColor Cyan
        Write-Host '  Cleanup completed successfully!' -ForegroundColor Green
        Write-Host '========================================' -ForegroundColor Cyan
        Write-Host ''
        exit 0
    }

    $FileList = $Files -join ';'
    Write-Info "Files: $($Files.Count) file(s) found"

    Write-Step 'Running JetBrains cleanup...'

    . "$ScriptDir\PowerShell\Modules\TimeStamp.ps1"

    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    jb cleanupcode .\Proxyfan.slnx --profile='Custom: Full Cleanup' --verbosity=WARN --no-build "--include=$FileList" | TimeStamp
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan

    if ($ExitCode -eq 0) {
        Write-Host "  Cleanup completed successfully! ($Elapsed)" -ForegroundColor Green
        Write-Host '========================================' -ForegroundColor Cyan
        Write-Host ''
        exit 0
    }

    Write-Host "  Cleanup failed ($Elapsed)" -ForegroundColor Red
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host ''
    exit 1
}
finally {
    Pop-Location
}
