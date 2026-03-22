<#
.SYNOPSIS
    Runs the Proxyfan test suite.

.DESCRIPTION
    Executes all tests in the solution using the TUnit framework with the
    Microsoft.Testing.Platform runner. Prints a summary of passed/failed
    counts on failure.

.PARAMETER Configuration
    MSBuild build configuration to run tests under. Defaults to 'Debug'.
    Use 'Release' to match the CI pipeline.

.PARAMETER NoBuild
    Skip the implicit build step and run tests against the last compiled
    output. Pass this flag when tests are invoked immediately after a
    successful build (e.g. from Invoke-Build.ps1 or Initialize-Repository.ps1).

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    .\.tools\Run-Tests.ps1
    Run the full test suite (Debug configuration).

.EXAMPLE
    .\.tools\Run-Tests.ps1 -Configuration Release
    Run tests under the Release configuration.

.EXAMPLE
    .\.tools\Run-Tests.ps1 -NoBuild
    Run tests without rebuilding first (fastest when the code is already built).
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

# ─── Formatting helpers ────────────────────────────────────────────────────────

function Write-Step    { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host '  [OK] '   -NoNewline -ForegroundColor Green;  Write-Host $Message }
function Write-Failure { param($Message) Write-Host '  [FAIL] ' -NoNewline -ForegroundColor Red;    Write-Host $Message }
function Write-Info    { param($Message) Write-Host "  $Message" -ForegroundColor Gray }

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Test Runner' -ForegroundColor Cyan
    Write-Host '====================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Run-Tests.ps1 [options]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Configuration   Build configuration: Debug (default) or Release'
    Write-Host '  -NoBuild         Skip rebuild and test against existing output'
    Write-Host '  -Help            Show this help message'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\.tools\Run-Tests.ps1                         # Run tests (Debug)'
    Write-Host '  .\.tools\Run-Tests.ps1 -Configuration Release  # Run tests (Release)'
    Write-Host '  .\.tools\Run-Tests.ps1 -NoBuild                # Skip rebuild'
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
    Write-Host '  Proxyfan Test Runner' -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan

    Write-Step "Running tests (configuration: $Configuration)..."
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $TestArgs = @('test', '--solution', 'Proxyfan.slnx', '-c', $Configuration, '-v', 'minimal')
    if ($NoBuild) { $TestArgs += '--no-build' }

    $Output   = & dotnet @TestArgs 2>&1
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan

    if ($ExitCode -eq 0) {
        Write-Host "  All tests passed! ($Elapsed)" -ForegroundColor Green
        Write-Host '========================================' -ForegroundColor Cyan
        Write-Host ''
        exit 0
    }

    Write-Host "  Tests failed ($Elapsed)" -ForegroundColor Red
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host ''

    $FailedTests = $Output | Where-Object { $_ -match '^\s*failed\s+\S+\.\S+' }
    if ($FailedTests) {
        Write-Info 'Failed tests:'
        $FailedTests | ForEach-Object { Write-Info "  $($_.Trim())" }
        Write-Host ''
    }

    $FailedProjects = $Output |
        Where-Object { $_ -match '\.dll \(' -and $_ -notmatch '\bpassed\b' -and $_ -notmatch 'Running tests from' } |
        ForEach-Object { $_.Trim() } |
        Select-Object -Unique
    if ($FailedProjects) {
        Write-Info 'Failed projects:'
        $FailedProjects | ForEach-Object {
            $Line = if ($_ -match '\\([^\\]+\.dll) \([^)]+\) (.+)$') { "$($Matches[1]) — $($Matches[2])" } else { $_ }
            Write-Info "  $Line"
        }
        Write-Host ''
    }

    $SummaryLines = $Output | Select-String -Pattern '^\s+(total:|failed:|error:|succeeded:|skipped:)' |
                    ForEach-Object { $_.Line.Trim() }
    if ($SummaryLines) {
        Write-Info 'Summary:'
        $SummaryLines | ForEach-Object { Write-Info "  $_" }
        Write-Host ''
    }

    exit 1
}
finally {
    Pop-Location
}
