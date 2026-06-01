#Requires -Version 7

<#
.SYNOPSIS
    Shared console output helpers for Proxyfan .tools scripts.

.DESCRIPTION
    Centralises the small family of timestamped, colour-coded console writers
    that have accreted in copy-paste form across .tools/*.ps1. Importing this
    module gives every script the same banner / step / status vocabulary so
    that output stays uniform for both humans and log-scraping coding agents.

    The module is intentionally narrow — output, banners, and stopwatch
    formatting only. It is NOT a general-purpose utility dumping ground.

    Functions exported:
        Write-Step       Cyan timestamped step header.
        Write-Success    Green '[OK]' status line.
        Write-Failure    Red   '[FAIL]' status line.
        Write-Warn       Yellow'[WARN]' status line.
        Write-Info       Gray  prefixed informational line.
        Write-Banner     Cyan banner box around a title.
        Format-Elapsed   Format a [System.TimeSpan] as 'mm:ss' or 'h:mm:ss'.

.EXAMPLE
    Import-Module "$PSScriptRoot/PowerShell/Modules/Output.psm1" -Force
    Write-Banner 'Proxyfan Build'
    Write-Step    'Restoring NuGet packages...'
    Write-Success 'Packages restored (00:42)'

.NOTES
    Imported with `-Force` from every .tools/*.ps1 entry-point so that ad-hoc
    edits to the module take effect on the next run without reloading the host.
#>

Set-StrictMode -Version Latest

function Write-Step {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [string] $Message)

    Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Success {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [string] $Message)

    Write-Host '  [OK] ' -NoNewline -ForegroundColor Green
    Write-Host $Message
}

function Write-Failure {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [string] $Message)

    Write-Host '  [FAIL] ' -NoNewline -ForegroundColor Red
    Write-Host $Message
}

function Write-Warn {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [string] $Message)

    Write-Host '  [WARN] ' -NoNewline -ForegroundColor Yellow
    Write-Host $Message
}

function Write-Info {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [string] $Message)

    Write-Host "  $Message" -ForegroundColor Gray
}

function Write-Banner {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)] [string] $Title,
        [int] $Width = 40
    )

    $line = '=' * [Math]::Max($Width, $Title.Length + 4)
    Write-Host ''
    Write-Host $line                  -ForegroundColor Cyan
    Write-Host ('  ' + $Title)        -ForegroundColor Cyan
    Write-Host $line                  -ForegroundColor Cyan
}

function Format-Elapsed {
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 0)] [System.TimeSpan] $Elapsed)

    if ($Elapsed.TotalHours -ge 1) {
        return $Elapsed.ToString('h\:mm\:ss')
    }
    return $Elapsed.ToString('mm\:ss')
}

Export-ModuleMember -Function `
    Write-Step, Write-Success, Write-Failure, Write-Warn, Write-Info, `
    Write-Banner, Format-Elapsed
