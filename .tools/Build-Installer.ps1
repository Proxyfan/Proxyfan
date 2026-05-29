<#
.SYNOPSIS
    Builds the Proxyfan portable ZIP distribution.

.DESCRIPTION
    Produces a Windows x64 self-contained build of the Proxyfan desktop client
    via 'dotnet publish' and compresses the output into a versioned ZIP
    suitable for the portable distribution channel described in
    docs/BACKLOG.md E12-F01-T01.

    The output is written to:
        artifacts/installer/Proxyfan-portable-<version>-win-x64.zip

    The script does not bundle WiX (MSI) or makeappx (MSIX). Those flows
    require Windows SDK / WiX Toolset and are tracked as follow-up work in
    BACKLOG.md.

.PARAMETER Configuration
    MSBuild configuration to publish. Defaults to 'Release'.

.PARAMETER OutputDirectory
    Directory the ZIP is written to. Defaults to '<repo-root>/artifacts/installer'.

.PARAMETER Version
    Version string embedded in the ZIP filename. Defaults to '0.0.0-local'.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1 -Version 1.2.3
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory,
    [string]$Version = '0.0.0-local',
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

Write-Section 'Proxyfan Portable Installer Build'

Write-Host "Configuration   : $Configuration"
Write-Host "Version         : $Version"
Write-Host "OutputDirectory : $OutputDirectory"
Write-Host ''

$PublishDirectory = Join-Path $RepositoryRoot 'artifacts\publish'
$ZipPath = Join-Path $OutputDirectory "Proxyfan-portable-$Version-win-x64.zip"

if (Test-Path $PublishDirectory) {
    Remove-Item -Recurse -Force $PublishDirectory
}
New-Item -ItemType Directory -Force -Path $PublishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

Write-Host '[1/2] Publishing Client.Desktop (win-x64, self-contained)...' -ForegroundColor Yellow

$DesktopProject = Join-Path $RepositoryRoot 'src\Clients\Client.Desktop\Client.Desktop.csproj'

# Clean obj caches to prevent cross-configuration state leaking from a prior Debug build
# (Avalonia.Diagnostics is conditionally referenced and its presence in obj cache can
# cause Debug builds to fail after a Release publish until obj is wiped).
Get-ChildItem -Path (Join-Path $RepositoryRoot 'src') -Directory -Recurse |
    Where-Object { $_.Name -eq 'obj' } |
    ForEach-Object { Remove-Item -Recurse -Force $_.FullName -ErrorAction SilentlyContinue }

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

Write-Host ''
Write-Host '[2/2] Compressing portable ZIP...' -ForegroundColor Yellow

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

Compress-Archive -Path (Join-Path $PublishDirectory '*') -DestinationPath $ZipPath -CompressionLevel Optimal

$ZipInfo = Get-Item $ZipPath
$SizeMb = [math]::Round($ZipInfo.Length / 1MB, 1)

Write-Section "Portable build succeeded"
Write-Host ('  ZIP  : ' + $ZipPath) -ForegroundColor Green
Write-Host ('  Size : ' + $SizeMb + ' MB') -ForegroundColor Green
Write-Host ''
