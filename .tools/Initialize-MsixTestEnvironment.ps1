<#
.SYNOPSIS
    One-shot setup for the MSIX-based UI automation test pipeline.

.DESCRIPTION
    Prepares the local machine to run the FlaUI UI automation tests
    (tests/Client.UiAutomationTests) in their canonical mode where every test
    installs a signed MSIX, runs through the test, then uninstalls.

    Steps performed (idempotent — safe to re-run):

      1. Verify the Windows 10 SDK is installed (provides MakeAppx + signtool).
         Install via winget if missing (requires elevation prompt only on first run).
      2. Create a self-signed test signing certificate with subject 'CN=Proxyfan'
         in CurrentUser\My if one does not already exist.
      3. Import that cert into LocalMachine\TrustedPeople AND LocalMachine\Root
         (REQUIRES ADMIN — the Add-AppxPackage MSIX install will fail without it).
      4. Build the MSIX via .tools/Build-MsixPackage.ps1.
      5. Sign the MSIX with the dev cert.

    After this script completes successfully, run the FlaUI tests via:
        dotnet run --project tests/Client.UiAutomationTests \
            --no-build -c Debug

    To opt out of the MSIX install/uninstall cycle (faster ~2 s/test direct .exe
    launch instead of ~18 s/test MSIX cycle), set the env var:
        $env:PROXYFAN_UI_TESTS_SKIP_MSIX = 'true'

.PARAMETER Configuration
    MSBuild configuration to publish/package. Defaults to Release.

.PARAMETER Version
    MSIX package version (4-segment Major.Minor.Build.Revision). Defaults to '0.0.1.0'.

.PARAMETER SkipSdkInstall
    Skip the Windows SDK installation step (when you know it is already installed).

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-MsixTestEnvironment.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Version = '0.0.1.0',
    [switch]$SkipSdkInstall,
    [switch]$Help
)

if ($Help) {
    Get-Help -Detailed $MyInvocation.MyCommand.Definition | Out-String | Write-Host
    return
}

$ErrorActionPreference = 'Stop'
$ScriptDir = $PSScriptRoot
$RepositoryRoot = Split-Path -Parent $ScriptDir

function Write-Section([string]$Message) {
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw 'This script must run elevated (the cert import step writes to LocalMachine cert stores). Re-run from an administrator PowerShell.'
}

# ─── 1. Windows SDK (provides MakeAppx + signtool) ────────────────────────────
Write-Section 'Step 1/5  Windows 10 SDK'

$sdkBin = 'C:\Program Files (x86)\Windows Kits\10\bin'
$haveSdk = (Test-Path $sdkBin) -and
    ((Get-ChildItem -Path $sdkBin -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue).Count -gt 0)

if ($haveSdk) {
    Write-Host '  Windows 10 SDK already installed.' -ForegroundColor Green
}
elseif ($SkipSdkInstall) {
    throw "Windows 10 SDK not found at '$sdkBin' but -SkipSdkInstall was specified. Install the SDK first or omit the flag."
}
else {
    Write-Host '  Installing Microsoft.WindowsSDK.10.0.26100 via winget...' -ForegroundColor Yellow
    & winget install --id 'Microsoft.WindowsSDK.10.0.26100' --silent --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0) {
        throw "winget install failed (exit $LASTEXITCODE). Install the Windows 10 SDK manually."
    }
    Write-Host '  Windows 10 SDK installed.' -ForegroundColor Green
}

# Resolve the newest installed makeappx + signtool for downstream use.
$sdkVersion = Get-ChildItem -Path $sdkBin -Directory |
    Where-Object { $_.Name -match '^10\.' } |
    Sort-Object Name -Descending |
    Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } |
    Select-Object -First 1
if (-not $sdkVersion) {
    throw "MakeAppx.exe not found under '$sdkBin' even after SDK install."
}
$makeAppx = Join-Path $sdkVersion.FullName 'x64\makeappx.exe'
$signtool = Join-Path $sdkVersion.FullName 'x64\signtool.exe'
Write-Host "  Using SDK version: $($sdkVersion.Name)" -ForegroundColor Gray
Write-Host "  makeappx: $makeAppx" -ForegroundColor Gray
Write-Host "  signtool: $signtool" -ForegroundColor Gray

# ─── 2. Self-signed certificate ───────────────────────────────────────────────
Write-Section 'Step 2/5  Self-signed test signing certificate'

$certSubject = 'CN=Proxyfan'
$existing = Get-ChildItem -Path 'Cert:\CurrentUser\My' -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq $certSubject } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($existing) {
    Write-Host "  Cert already exists. Thumbprint: $($existing.Thumbprint)" -ForegroundColor Green
    $cert = $existing
}
else {
    Write-Host '  Creating self-signed cert...' -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $certSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName 'Proxyfan Test Signing' `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    Write-Host "  Created cert. Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
}

$pfxPath = Join-Path $env:TEMP 'proxyfan-test-cert.pfx'
$cerPath = Join-Path $env:TEMP 'proxyfan-test-cert.cer'
$password = ConvertTo-SecureString -String 'TestOnly!' -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
Write-Host "  Exported PFX: $pfxPath" -ForegroundColor Gray

# ─── 3. Import into trusted stores ────────────────────────────────────────────
Write-Section 'Step 3/5  Trust the cert in LocalMachine stores'

foreach ($store in 'TrustedPeople', 'Root') {
    $alreadyTrusted = Get-ChildItem -Path "Cert:\LocalMachine\$store" -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
    if ($alreadyTrusted) {
        Write-Host "  Cert already in LocalMachine\$store." -ForegroundColor Green
    }
    else {
        Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation "Cert:\LocalMachine\$store" -Password $password | Out-Null
        Write-Host "  Imported into LocalMachine\$store." -ForegroundColor Green
    }
}

# ─── 4. Build the MSIX ────────────────────────────────────────────────────────
Write-Section "Step 4/5  Build MSIX (configuration: $Configuration, version: $Version)"

& "$ScriptDir\Build-MsixPackage.ps1" -Configuration $Configuration -Version $Version
if ($LASTEXITCODE -ne 0) {
    throw "Build-MsixPackage.ps1 failed (exit $LASTEXITCODE)."
}

$msixPath = Join-Path $RepositoryRoot "artifacts\installer\Proxyfan-$Version-win-x64.msix"
if (-not (Test-Path $msixPath)) {
    throw "MSIX not found at '$msixPath' after build."
}

# ─── 5. Sign the MSIX ─────────────────────────────────────────────────────────
Write-Section 'Step 5/5  Sign the MSIX'

& $signtool sign /fd SHA256 /a /f $pfxPath /p 'TestOnly!' $msixPath
if ($LASTEXITCODE -ne 0) {
    throw "signtool failed (exit $LASTEXITCODE)."
}
Write-Host '  MSIX signed successfully.' -ForegroundColor Green

Write-Section 'MSIX test environment ready'
Write-Host "  Package: $msixPath" -ForegroundColor Green
Write-Host '' -NoNewline
Write-Host 'Run the FlaUI tests via the canonical MSIX pipeline:' -ForegroundColor Cyan
Write-Host '  dotnet run --project tests/Client.UiAutomationTests --no-build -c Debug' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Or opt out of MSIX per-test cycles for faster iteration (~2 s/test):' -ForegroundColor DarkGray
Write-Host '  $env:PROXYFAN_UI_TESTS_SKIP_MSIX = ''true''' -ForegroundColor DarkGray
Write-Host ''
