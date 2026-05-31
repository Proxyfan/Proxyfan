<#
.SYNOPSIS
    Builds a Proxyfan MSIX package from the freshly published Client.Desktop output.

.DESCRIPTION
    Scaffolding for an MSIX packaging pipeline (separate from the existing
    MSI pipeline produced by Build-Installer.ps1). Wires together:

      1. dotnet publish -r win-x64 --self-contained  (reuse Build-Installer logic)
      2. Stage publish output + installer/Proxyfan.appxmanifest into a temp layout
      3. MakeAppx.exe pack /d <layout> /p <output>.msix
      4. (Developer) sign with a self-signed cert added to LocalMachine\TrustedPeople

    Requires the Windows 10 SDK 10.0.19041.0+ on PATH (provides MakeAppx.exe
    and SignTool.exe). Falls back with a clear error message when the SDK is
    missing.

    Used by tests/Client.UiAutomationTests in the (not-yet-wired) MSIX install
    mode. The default test mode uses LOCALAPPDATA redirection instead.

.PARAMETER Configuration
    MSBuild configuration. Defaults to Release.

.PARAMETER OutputDirectory
    Where to write the .msix. Defaults to artifacts/installer.

.PARAMETER Version
    Version embedded in the package manifest. Must be a 4-segment
    Major.Minor.Build.Revision form (per MSIX schema). Defaults to '0.0.1.0'.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-MsixPackage.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory,
    [string]$Version = '0.0.1.0',
    [switch]$Help
)

if ($Help) {
    Get-Help -Detailed $MyInvocation.MyCommand.Definition | Out-String | Write-Host
    return
}

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\installer'
}

function Write-Section([string]$Message) {
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan
}

function Resolve-MakeAppxPath {
    # MakeAppx.exe lives under the Windows 10 SDK installation. Search common
    # SDK roots for the newest version of MakeAppx.exe.
    $sdkRoot = 'C:\Program Files (x86)\Windows Kits\10\bin'
    if (-not (Test-Path $sdkRoot)) {
        throw "Windows 10 SDK not found at '$sdkRoot'. Install the Windows 10 SDK 10.0.19041.0 or later."
    }

    $versionDirs = Get-ChildItem -Path $sdkRoot -Directory |
        Where-Object { $_.Name -match '^10\.' } |
        Sort-Object -Property Name -Descending
    foreach ($versionDir in $versionDirs) {
        $candidate = Join-Path $versionDir.FullName 'x64\makeappx.exe'
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "MakeAppx.exe not found under '$sdkRoot'. Install the Windows 10 SDK packaging tools."
}

Write-Section 'Proxyfan MSIX Package Build'

Write-Host "Configuration   : $Configuration"
Write-Host "Version         : $Version"
Write-Host "OutputDirectory : $OutputDirectory"
Write-Host ''

# 1) Publish the desktop app self-contained for win-x64.
$PublishDirectory = Join-Path $RepositoryRoot 'artifacts\publish'
if (Test-Path $PublishDirectory) {
    Remove-Item -Recurse -Force $PublishDirectory
}
New-Item -ItemType Directory -Force -Path $PublishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$DesktopProject = Join-Path $RepositoryRoot 'src\Clients\Client.Desktop\Client.Desktop.csproj'

Write-Host '[1/3] Publishing Client.Desktop (win-x64, self-contained)...' -ForegroundColor Yellow

& dotnet publish $DesktopProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    --output $PublishDirectory `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# 2) Stage MSIX layout: publish output + manifest + Assets placeholder.
Write-Host ''
Write-Host '[2/3] Staging MSIX layout...' -ForegroundColor Yellow

$StagingDirectory = Join-Path $RepositoryRoot 'artifacts\msix-staging'
if (Test-Path $StagingDirectory) {
    Remove-Item -Recurse -Force $StagingDirectory
}
New-Item -ItemType Directory -Force -Path $StagingDirectory | Out-Null

# Copy publish payload.
Copy-Item -Path (Join-Path $PublishDirectory '*') -Destination $StagingDirectory -Recurse

# Copy and version-fix the manifest.
$ManifestSource = Join-Path $RepositoryRoot 'installer\Proxyfan.appxmanifest'
$ManifestTarget = Join-Path $StagingDirectory 'AppxManifest.xml'
$manifestContent = Get-Content $ManifestSource -Raw
$manifestContent = $manifestContent -replace 'Version="0\.0\.1\.0"', "Version=`"$Version`""
Set-Content -Path $ManifestTarget -Value $manifestContent -NoNewline

# Create placeholder logo assets if not provided yet.
$AssetsDirectory = Join-Path $StagingDirectory 'Assets'
New-Item -ItemType Directory -Force -Path $AssetsDirectory | Out-Null
foreach ($logo in @('StoreLogo.png', 'Square150x150Logo.png', 'Square44x44Logo.png', 'Wide310x150Logo.png')) {
    $target = Join-Path $AssetsDirectory $logo
    if (-not (Test-Path $target)) {
        # 1x1 transparent PNG sentinel — works for sideload install; replace
        # with real assets before any production release.
        $bytes = [System.Convert]::FromBase64String(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=')
        [System.IO.File]::WriteAllBytes($target, $bytes)
    }
}

# 3) Build the .msix package.
Write-Host ''
Write-Host '[3/3] Packing MSIX via MakeAppx...' -ForegroundColor Yellow

$MakeAppx = Resolve-MakeAppxPath
$MsixPath = Join-Path $OutputDirectory "Proxyfan-$Version-win-x64.msix"
if (Test-Path $MsixPath) {
    Remove-Item -Force $MsixPath
}

& $MakeAppx pack /d $StagingDirectory /p $MsixPath /nv /o
if ($LASTEXITCODE -ne 0) {
    throw "MakeAppx pack failed with exit code $LASTEXITCODE"
}

Write-Section 'MSIX build succeeded'
Write-Host ('  Package : ' + $MsixPath) -ForegroundColor Green
$msixInfo = Get-Item $MsixPath
Write-Host ('  Size    : {0:F1} MB' -f ($msixInfo.Length / 1MB)) -ForegroundColor Green
Write-Host ''
Write-Host 'To install for testing (self-signed cert required):' -ForegroundColor DarkGray
Write-Host '    Add-AppxPackage -Path ''<path>.msix'' -AllowUnsigned' -ForegroundColor DarkGray
Write-Host 'AppUserModelId once installed: Proxyfan.Proxyfan_<publisher-hash>!App' -ForegroundColor DarkGray
