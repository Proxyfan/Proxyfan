<#
.SYNOPSIS
    Builds the Proxyfan portable ZIP distribution and (optionally) MSI installer.

.DESCRIPTION
    Produces a Windows x64 self-contained build of the Proxyfan desktop client
    via 'dotnet publish' and compresses the output into a versioned ZIP
    suitable for the portable distribution channel described in
    docs/BACKLOG.md E12-F01-T01.

    When -BuildMsi is supplied, the script also invokes the WiX toolset
    (installed via 'dotnet tool install --global wix') to produce an MSI
    installer from installer/Proxyfan.wxs.

    The output is written to:
        artifacts/installer/Proxyfan-portable-<version>-win-x64.zip
        artifacts/installer/Proxyfan-<version>-win-x64.msi  (when -BuildMsi)

.PARAMETER Configuration
    MSBuild configuration to publish. Defaults to 'Release'.

.PARAMETER OutputDirectory
    Directory the ZIP/MSI are written to. Defaults to '<repo-root>/artifacts/installer'.

.PARAMETER Version
    Version string embedded in the artifact filename. Defaults to '0.0.0-local'.
    For MSI builds this must be a valid MSI version of the form X.Y.Z or X.Y.Z.W;
    'local'/SemVer pre-release suffixes are stripped to the leading numeric part.

.PARAMETER BuildMsi
    When supplied, build the MSI installer in addition to the portable ZIP.
    Requires the 'wix' dotnet global tool: 'dotnet tool install --global wix'.

.PARAMETER Help
    Display this help message and exit.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1 -Version 1.2.3 -BuildMsi
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory,
    [string]$Version = '0.0.0-local',
    [switch]$BuildMsi,
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

function ConvertTo-MsiProductVersion([string]$VersionString) {
    # MSI ProductVersion must be in the form major.minor.build[.revision] with each
    # component a non-negative integer. Strip any SemVer pre-release/build suffix
    # ('-local', '+sha', etc.) and pad missing segments with '0'.
    $core = $VersionString
    foreach ($separator in @('-', '+', '_')) {
        $idx = $core.IndexOf($separator)
        if ($idx -ge 0) {
            $core = $core.Substring(0, $idx)
        }
    }
    $parts = $core -split '\.' | Where-Object { $_ -match '^\d+$' }
    while ($parts.Count -lt 3) {
        $parts += '0'
    }
    if ($parts.Count -gt 4) {
        $parts = $parts[0..3]
    }
    return ($parts -join '.')
}

Write-Section 'Proxyfan Portable Installer Build'

Write-Host "Configuration   : $Configuration"
Write-Host "Version         : $Version"
Write-Host "OutputDirectory : $OutputDirectory"
Write-Host "BuildMsi        : $BuildMsi"
Write-Host ''

$PublishDirectory = Join-Path $RepositoryRoot 'artifacts\publish'
$ZipPath = Join-Path $OutputDirectory "Proxyfan-portable-$Version-win-x64.zip"

if (Test-Path $PublishDirectory) {
    Remove-Item -Recurse -Force $PublishDirectory
}
New-Item -ItemType Directory -Force -Path $PublishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

Write-Host '[1/3] Publishing Client.Desktop (win-x64, self-contained)...' -ForegroundColor Yellow

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
Write-Host '[2/3] Compressing portable ZIP...' -ForegroundColor Yellow

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

Compress-Archive -Path (Join-Path $PublishDirectory '*') -DestinationPath $ZipPath -CompressionLevel Optimal

$ZipInfo = Get-Item $ZipPath
$ZipSizeMb = [math]::Round($ZipInfo.Length / 1MB, 1)

if ($BuildMsi) {
    Write-Host ''
    Write-Host '[3/3] Building MSI installer with WiX...' -ForegroundColor Yellow

    $WixSource = Join-Path $RepositoryRoot 'installer\Proxyfan.wxs'
    $MsiPath = Join-Path $OutputDirectory "Proxyfan-$Version-win-x64.msi"
    $MsiProductVersion = ConvertTo-MsiProductVersion -VersionString $Version

    if (Test-Path $MsiPath) {
        Remove-Item -Force $MsiPath
    }

    $WixCommand = Get-Command 'wix' -ErrorAction SilentlyContinue
    if (-not $WixCommand) {
        throw "The 'wix' dotnet global tool is not on PATH. Install it with 'dotnet tool install --global wix'."
    }

    & wix build $WixSource `
        -arch x64 `
        -define "ProductVersion=$MsiProductVersion" `
        -define "PublishSourceDirectory=$PublishDirectory" `
        -bindpath $PublishDirectory `
        -out $MsiPath

    if ($LASTEXITCODE -ne 0) {
        throw "wix build failed with exit code $LASTEXITCODE"
    }

    $MsiInfo = Get-Item $MsiPath
    $MsiSizeMb = [math]::Round($MsiInfo.Length / 1MB, 1)
}
else {
    Write-Host ''
    Write-Host '[3/3] Skipping MSI build (-BuildMsi not specified).' -ForegroundColor DarkGray
}

Write-Section "Build succeeded"
Write-Host ('  ZIP  : ' + $ZipPath) -ForegroundColor Green
Write-Host ('  Size : ' + $ZipSizeMb + ' MB') -ForegroundColor Green
if ($BuildMsi) {
    Write-Host ('  MSI  : ' + $MsiPath) -ForegroundColor Green
    Write-Host ('  Size : ' + $MsiSizeMb + ' MB') -ForegroundColor Green
}
Write-Host ''
