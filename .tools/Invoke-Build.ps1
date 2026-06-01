<#
.SYNOPSIS
    Builds the Proxyfan solution.

.DESCRIPTION
    This is the canonical build script for the Proxyfan project. It must be used
    instead of calling 'dotnet build' directly.

    Steps (in order):
      1. Restore NuGet packages          (skipped with -SkipRestore)
      2. Build the solution              (-NoIncremental forces --no-incremental)
      3. Validate resource keys          (Test-ResourceKeys.ps1)
      4. Markdown gate                   (opt-in with -CheckMarkdown)
      5. Run tests                       (opt-in with -RunTests)

.PARAMETER RunTests
    Run the full test suite after building.
    Delegates to .tools\Run-Tests.ps1 -NoBuild.

.PARAMETER SkipRestore
    Skip 'dotnet restore'. Use when NuGet packages have not changed to
    speed up incremental builds.

.PARAMETER NoIncremental
    Pass --no-incremental to 'dotnet build' to force a full recompile of
    every project. Use this for a pre-merge sanity check or when you suspect
    MSBuild's incremental cache is stale; it does NOT clean bin/obj or
    refresh packages on its own. Combine with -SkipRestore as you would
    during normal incremental development.

.PARAMETER CheckMarkdown
    Run .tools\Invoke-MarkdownGate.ps1 after the build to enforce the
    per-category size limits and the freshness window for agent-loaded
    docs. Opt-in by design — markdown freshness is not a correctness gate
    and must not block unrelated changes by default.

.PARAMETER Configuration
    MSBuild build configuration. Defaults to 'Debug'.
    Use 'Release' to mirror the CI pipeline.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    .\.tools\Invoke-Build.ps1
    Standard Debug build.

.EXAMPLE
    .\.tools\Invoke-Build.ps1 -RunTests
    Build and run the full test suite.

.EXAMPLE
    .\.tools\Invoke-Build.ps1 -RunTests -Configuration Release
    Full Release build mirroring the CI pipeline.

.EXAMPLE
    .\.tools\Invoke-Build.ps1 -SkipRestore
    Incremental build when NuGet packages have not changed.

.EXAMPLE
    .\.tools\Invoke-Build.ps1 -NoIncremental -RunTests
    Full cold recompile + test (use before a pre-merge cold check).

.EXAMPLE
    .\.tools\Invoke-Build.ps1 -CheckMarkdown
    Standard build plus the markdown size + freshness gate.
#>

[CmdletBinding()]
param(
    [switch]$RunTests,
    [switch]$SkipRestore,
    [switch]$NoIncremental,
    [switch]$CheckMarkdown,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

# ─── Formatting helpers ────────────────────────────────────────────────────────
# Sourced from .tools\PowerShell\Modules\Output.psm1 so that every script speaks
# the same vocabulary. Local fallback definitions retained for the rare case the
# module is unavailable (e.g. shallow checkout in a downstream consumer).
$OutputModule = Join-Path $PSScriptRoot 'PowerShell/Modules/Output.psm1'
if (Test-Path -LiteralPath $OutputModule) {
    Import-Module $OutputModule -Force
} else {
    function Write-Step   { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
    function Write-Success { param($Message) Write-Host '  [OK] '   -NoNewline -ForegroundColor Green;  Write-Host $Message }
    function Write-Failure { param($Message) Write-Host '  [FAIL] ' -NoNewline -ForegroundColor Red;    Write-Host $Message }
    function Write-Warn    { param($Message) Write-Host '  [WARN] ' -NoNewline -ForegroundColor Yellow; Write-Host $Message }
    function Write-Info    { param($Message) Write-Host "  $Message" -ForegroundColor Gray }
}

# ─── Build steps ──────────────────────────────────────────────────────────────

function Invoke-Restore {
    Write-Step 'Restoring NuGet packages...'
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $Output   = dotnet restore Proxyfan.slnx 2>&1
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($ExitCode -eq 0) {
        Write-Success "Packages restored ($Elapsed)"
        return $true
    }

    Write-Failure "Package restore failed ($Elapsed)"
    Write-Info ($Output | Select-Object -Last 20 | Out-String)
    return $false
}

function Invoke-Build {
    $mode = if ($NoIncremental) { 'cold' } else { 'incremental' }
    Write-Step "Building solution (configuration: $Configuration, mode: $mode)..."
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $BuildArgs = @('build', 'Proxyfan.slnx', '--no-restore', '-c', $Configuration)
    if ($NoIncremental) { $BuildArgs += '--no-incremental' }

    $Output   = & dotnet @BuildArgs 2>&1
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($ExitCode -eq 0) {
        Write-Success "Build succeeded ($Elapsed)"
        return $true
    }

    Write-Failure "Build failed ($Elapsed)"
    Write-Info 'Last 20 lines of output:'
    Write-Host ($Output | Select-Object -Last 20 | Out-String) -ForegroundColor Gray
    return $false
}

function Invoke-MarkdownGate {
    Write-Step 'Running markdown gate...'
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    & "$ScriptDir\Invoke-MarkdownGate.ps1"
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($ExitCode -eq 0) {
        Write-Success "Markdown gate passed ($Elapsed)"
        return $true
    }
    Write-Failure "Markdown gate failed ($Elapsed)"
    return $false
}

function Invoke-Tests {
    Write-Step 'Running tests...'
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    & "$ScriptDir\Run-Tests.ps1" -NoBuild -Configuration $Configuration
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($ExitCode -eq 0) {
        Write-Success "Tests passed ($Elapsed)"
        return $true
    }

    Write-Failure "Tests failed ($Elapsed)"
    return $false
}

function Invoke-ResourceValidation {
    Write-Step 'Validating resource keys...'
    $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    & "$ScriptDir\Test-ResourceKeys.ps1" -Path $RepoRoot | Out-Null
    $ExitCode = $LASTEXITCODE

    $Stopwatch.Stop()
    $Elapsed = $Stopwatch.Elapsed.ToString('mm\:ss')

    if ($ExitCode -eq 0) {
        Write-Success "Resource keys validated ($Elapsed)"
        return $true
    }

    Write-Failure "Resource key validation failed ($Elapsed)"
    return $false
}

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan Build Script' -ForegroundColor Cyan
    Write-Host '=====================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Invoke-Build.ps1 [options]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -RunTests          Run the full test suite after building'
    Write-Host '  -SkipRestore       Skip dotnet restore (faster incremental builds)'
    Write-Host '  -NoIncremental     Force --no-incremental (full recompile; does not clean bin/obj)'
    Write-Host '  -CheckMarkdown     Also run the markdown size + freshness gate'
    Write-Host '  -Configuration     Build configuration: Debug (default) or Release'
    Write-Host '  -Help              Show this help message'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  .\.tools\Invoke-Build.ps1                                    # Standard build'
    Write-Host '  .\.tools\Invoke-Build.ps1 -RunTests                          # Build + test'
    Write-Host '  .\.tools\Invoke-Build.ps1 -RunTests -Configuration Release   # Full Release build'
    Write-Host '  .\.tools\Invoke-Build.ps1 -SkipRestore                       # Incremental build'
    Write-Host '  .\.tools\Invoke-Build.ps1 -NoIncremental -RunTests           # Cold compile + test'
    Write-Host '  .\.tools\Invoke-Build.ps1 -CheckMarkdown                     # Plus markdown gate'
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
    $TotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $AllSucceeded   = $true

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host '  Proxyfan Build' -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan

    # Step 1: Restore
    if ($SkipRestore) {
        Write-Step 'Skipping package restore (-SkipRestore)'
    } else {
        if (-not (Invoke-Restore)) {
            Write-Host "`nBuild failed: package restore failed." -ForegroundColor Red
            exit 1
        }
    }

    # Step 2: Build
    if (-not (Invoke-Build)) {
        Write-Host "`nBuild failed." -ForegroundColor Red
        exit 1
    }

    # Step 2b: Resource key validation
    if (-not (Invoke-ResourceValidation)) {
        Write-Host "`nResource validation failed." -ForegroundColor Red
        exit 1
    }

    # Step 2c: Markdown gate (opt-in)
    if ($CheckMarkdown) {
        if (-not (Invoke-MarkdownGate)) {
            Write-Host "`nMarkdown gate failed." -ForegroundColor Red
            exit 1
        }
    }

    # Step 3: Tests (optional)
    if ($RunTests) {
        if (-not (Invoke-Tests)) { $AllSucceeded = $false }
    }

    $TotalStopwatch.Stop()
    $TotalElapsed = $TotalStopwatch.Elapsed.ToString('mm\:ss')

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    if ($AllSucceeded) {
        Write-Host "  Build completed successfully! ($TotalElapsed)" -ForegroundColor Green
    } else {
        Write-Host "  Build completed with failures ($TotalElapsed)" -ForegroundColor Red
    }
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host ''

    exit $(if ($AllSucceeded) { 0 } else { 1 })
}
finally {
    Pop-Location
}
