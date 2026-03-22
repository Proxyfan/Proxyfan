<#
.SYNOPSIS
    Initializes a developer machine for working on the Proxyfan project.

.DESCRIPTION
    This script sets up the development environment by:
    - Verifying .NET 10 SDK is installed
    - Restoring required workloads (Android, iOS, WASM)
    - Restoring NuGet packages
    - Installing required .NET global tools
    - Building the solution

.PARAMETER SkipWorkloads
    Skip workload restoration. Use this for faster setup if you only need
    to work on desktop projects (WPF, Avalonia Desktop).

.PARAMETER SkipTools
    Skip restoration of required .NET local tools.

.PARAMETER RunTests
    Run the test suite after building.

.PARAMETER Help
    Display this help message.

.EXAMPLE
    .\.tools\Initialize-Repository.ps1
    Full setup with workloads, restore, and build.

.EXAMPLE
    .\.tools\Initialize-Repository.ps1 -SkipWorkloads
    Skip workload restore for desktop-only development.

.EXAMPLE
    .\.tools\Initialize-Repository.ps1 -SkipTools
    Skip .NET global tool installation.

.EXAMPLE
    .\.tools\Initialize-Repository.ps1 -RunTests
    Full setup and run tests.
#>

[CmdletBinding()]
param(
    [switch]$SkipWorkloads,
    [switch]$SkipTools,
    [switch]$RunTests,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Colors and formatting
function Write-Step { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "  [OK] " -NoNewline -ForegroundColor Green; Write-Host $Message }
function Write-Failure { param($Message) Write-Host "  [FAIL] " -NoNewline -ForegroundColor Red; Write-Host $Message }
function Write-Warning { param($Message) Write-Host "  [WARN] " -NoNewline -ForegroundColor Yellow; Write-Host $Message }
function Write-Info { param($Message) Write-Host "  $Message" -ForegroundColor Gray }

function Show-Help {
    Write-Host ""
    Write-Host "Proxyfan Developer Setup Script" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\.tools\Initialize-Repository.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -SkipWorkloads  Skip workload restoration (faster, desktop-only dev)"
    Write-Host "  -SkipTools      Skip .NET local tool restoration"
    Write-Host "  -RunTests       Run tests after building"
    Write-Host "  -Help           Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\.tools\Initialize-Repository.ps1                   # Full setup"
    Write-Host "  .\.tools\Initialize-Repository.ps1 -SkipWorkloads    # Desktop-only (no Android/iOS/WASM)"
    Write-Host "  .\.tools\Initialize-Repository.ps1 -SkipTools         # Skip .NET global tool install"
    Write-Host "  .\.tools\Initialize-Repository.ps1 -RunTests         # Full setup + run tests"
    Write-Host ""
}

function Test-DotNetSdk {
    Write-Step "Checking .NET SDK..."

    try {
        $dotnetVersion = dotnet --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed"
        }

        $majorVersion = [int]($dotnetVersion -split '\.')[0]
        if ($majorVersion -ge 10) {
            Write-Success ".NET SDK $dotnetVersion found"
            return $true
        } else {
            Write-Failure ".NET SDK $dotnetVersion found, but .NET 10+ is required"
            Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/10.0"
            return $false
        }
    }
    catch {
        Write-Failure ".NET SDK not found"
        Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/10.0"
        return $false
    }
}

function Restore-Workloads {
    Write-Step "Restoring workloads (Android, iOS, WASM)..."
    Write-Info "This may take a few minutes on first run..."

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $output = dotnet workload restore Proxyfan.slnx 2>&1
    $exitCode = $LASTEXITCODE

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.ToString("mm\:ss")

    if ($exitCode -eq 0) {
        Write-Success "Workloads restored ($elapsed)"
        return $true
    } else {
        Write-Failure "Workload restore failed ($elapsed)"
        Write-Info "Output: $output"
        Write-Info ""
        Write-Info "Try running manually with elevated permissions:"
        Write-Info "  dotnet workload restore Proxyfan.slnx"
        return $false
    }
}

function Restore-Packages {
    Write-Step "Restoring NuGet packages..."

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $output = dotnet restore Proxyfan.slnx 2>&1
    $exitCode = $LASTEXITCODE

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.ToString("mm\:ss")

    if ($exitCode -eq 0) {
        Write-Success "Packages restored ($elapsed)"
        return $true
    } else {
        Write-Failure "Package restore failed ($elapsed)"
        Write-Info "Output: $output"
        return $false
    }
}

function Build-Solution {
    Write-Step "Building solution..."

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $output = dotnet build Proxyfan.slnx --no-restore 2>&1
    $exitCode = $LASTEXITCODE

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.ToString("mm\:ss")

    if ($exitCode -eq 0) {
        Write-Success "Build succeeded ($elapsed)"
        return $true
    } else {
        Write-Failure "Build failed ($elapsed)"
        # Show last 20 lines of output for errors
        $errorLines = ($output | Select-Object -Last 20) -join "`n"
        Write-Info "Last 20 lines of output:"
        Write-Host $errorLines -ForegroundColor Gray
        return $false
    }
}

function Install-Tools {
    Write-Step "Restoring .NET local tools..."

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $output = dotnet tool restore 2>&1
    $exitCode = $LASTEXITCODE

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.ToString("mm\:ss")

    if ($exitCode -eq 0) {
        Write-Success "Local tools restored ($elapsed)"
        return $true
    } else {
        Write-Failure "Tool restore failed ($elapsed)"
        Write-Info "Output: $output"
        Write-Info "Run manually: dotnet tool restore"
        return $false
    }
}

function Invoke-Tests {
    Write-Step "Running tests..."

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    & "$scriptDir\Run-Tests.ps1" -NoBuild
    $exitCode = $LASTEXITCODE

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed.ToString("mm\:ss")

    if ($exitCode -eq 0) {
        Write-Success "Tests passed ($elapsed)"
        return $true
    } else {
        Write-Failure "Tests failed ($elapsed)"
        return $false
    }
}

# Main execution
if ($Help) {
    Show-Help
    exit 0
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Proxyfan Developer Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Change to repo root (script is in .tools/)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Push-Location $repoRoot

try {
    $totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $allSucceeded = $true

    # Step 1: Check .NET SDK
    if (-not (Test-DotNetSdk)) {
        Write-Host "`nSetup failed: .NET 10 SDK is required." -ForegroundColor Red
        exit 1
    }

    # Step 2: Restore workloads (optional)
    if ($SkipWorkloads) {
        Write-Step "Skipping workload restoration (-SkipWorkloads)"
        Write-Warning "Android, iOS, and Browser projects may not build"
    } else {
        if (-not (Restore-Workloads)) {
            Write-Warning "Workload restore failed, continuing anyway..."
            Write-Info "Desktop projects should still work"
        }
    }

    # Step 3: Restore packages
    if (-not (Restore-Packages)) {
        Write-Host "`nSetup failed: Package restore failed." -ForegroundColor Red
        exit 1
    }

    # Step 4: Install .NET global tools (optional)
    if ($SkipTools) {
        Write-Step "Skipping .NET local tool restoration (-SkipTools)"
    } else {
        if (-not (Install-Tools)) {
            Write-Warning "Tool installation failed, continuing anyway..."
        }
    }

    # Step 5: Build solution
    if (-not (Build-Solution)) {
        $allSucceeded = $false
        Write-Warning "Build failed, but packages are restored"
        Write-Info "You can try building individual projects"
    }

    # Step 6: Run tests (optional)
    if ($RunTests -and $allSucceeded) {
        if (-not (Invoke-Tests)) {
            $allSucceeded = $false
        }
    }

    $totalStopwatch.Stop()
    $totalElapsed = $totalStopwatch.Elapsed.ToString("mm\:ss")

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    if ($allSucceeded) {
        Write-Host "  Setup completed successfully! ($totalElapsed)" -ForegroundColor Green
    } else {
        Write-Host "  Setup completed with warnings ($totalElapsed)" -ForegroundColor Yellow
    }
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    if ($allSucceeded) {
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "  - Open Proxyfan.slnx in your IDE"
        Write-Host ""
    }

    exit $(if ($allSucceeded) { 0 } else { 1 })
}
finally {
    Pop-Location
}
