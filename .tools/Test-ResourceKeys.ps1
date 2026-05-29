<#
.SYNOPSIS
    Validates that all localized .resx files contain the same set of keys as
    their default (English) baseline.

.DESCRIPTION
    Walks the source tree under src/ for *.resx files named
    "<Baseline>.resx" (default culture) and "<Baseline>.<locale>.resx"
    (translations). For every translation file:
      * Missing keys (in baseline but not in translation) are reported as
        warnings — they remain renderable via the default culture fallback.
      * Extra keys (in translation but not in baseline) are reported as
        errors — the script exits non-zero so CI fails.

    Empty translation values are also reported as warnings to surface
    untranslated entries.

.PARAMETER Path
    The repository root to walk. Defaults to the parent directory of this
    script.

.PARAMETER WarningAsError
    Treat missing keys and empty translation values as errors instead of
    warnings.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Test-ResourceKeys.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Test-ResourceKeys.ps1 -WarningAsError
#>

[CmdletBinding()]
param(
    [string]$Path,
    [switch]$WarningAsError
)

$ErrorActionPreference = 'Stop'

function Write-Step    { param($Message) Write-Host "`n[$((Get-Date).ToString('HH:mm:ss'))] " -NoNewline; Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host '  [OK] '   -NoNewline -ForegroundColor Green;  Write-Host $Message }
function Write-Failure { param($Message) Write-Host '  [FAIL] ' -NoNewline -ForegroundColor Red;    Write-Host $Message }
function Write-Warn    { param($Message) Write-Host '  [WARN] ' -NoNewline -ForegroundColor Yellow; Write-Host $Message }
function Write-Info    { param($Message) Write-Host "  $Message" -ForegroundColor Gray }

function Get-ResourceKeys {
    param([string]$ResxPath)
    [xml]$xml = Get-Content -LiteralPath $ResxPath -Raw -Encoding UTF8
    $entries = @{}
    foreach ($node in $xml.root.data) {
        if (-not $node.name) { continue }
        $value = if ($null -eq $node.value) { '' } else { [string]$node.value }
        $entries[$node.name] = $value
    }
    return $entries
}

function Get-BaselineForFile {
    param([string]$FilePath)
    $name = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    $dotIndex = $name.LastIndexOf('.')
    if ($dotIndex -lt 0) { return $null }
    $stem = $name.Substring(0, $dotIndex)
    $locale = $name.Substring($dotIndex + 1)
    if ([string]::IsNullOrWhiteSpace($locale)) { return $null }
    if ($locale -notmatch '^[a-z]{2,3}(-[A-Za-z0-9]+)*$') { return $null }
    $baselinePath = Join-Path $directory ("{0}.resx" -f $stem)
    if (-not (Test-Path -LiteralPath $baselinePath)) { return $null }
    return [pscustomobject]@{
        Locale       = $locale
        BaselinePath = $baselinePath
    }
}

if (-not $Path) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $Path      = Split-Path -Parent $ScriptDir
}

Push-Location $Path
try {
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host '  Proxyfan Resource Key Validation' -ForegroundColor Cyan
    Write-Host '========================================' -ForegroundColor Cyan

    Write-Step "Scanning $Path for .resx files..."
    $resxFiles = Get-ChildItem -Path (Join-Path $Path 'src') -Filter '*.resx' -Recurse -File -ErrorAction SilentlyContinue
    if (-not $resxFiles) {
        Write-Info 'No .resx files found.'
        exit 0
    }

    $translationFiles = @()
    foreach ($file in $resxFiles) {
        $info = Get-BaselineForFile -FilePath $file.FullName
        if ($info) {
            $translationFiles += [pscustomobject]@{
                Path         = $file.FullName
                Locale       = $info.Locale
                BaselinePath = $info.BaselinePath
            }
        }
    }

    Write-Info ("Found {0} baseline .resx file(s), {1} translation file(s)." -f ($resxFiles.Count - $translationFiles.Count), $translationFiles.Count)

    if ($translationFiles.Count -eq 0) {
        Write-Success 'No translation files to validate (single-locale repository).'
        exit 0
    }

    $totalErrors   = 0
    $totalWarnings = 0

    foreach ($translation in $translationFiles) {
        $relativePath = $translation.Path.Substring($Path.Length).TrimStart('\','/')
        Write-Step "Validating $relativePath (locale: $($translation.Locale))..."
        $baselineKeys    = Get-ResourceKeys -ResxPath $translation.BaselinePath
        $translationKeys = Get-ResourceKeys -ResxPath $translation.Path

        $missingKeys = @($baselineKeys.Keys | Where-Object { -not $translationKeys.ContainsKey($_) })
        $extraKeys   = @($translationKeys.Keys | Where-Object { -not $baselineKeys.ContainsKey($_) })
        $emptyValues = @($translationKeys.Keys | Where-Object { [string]::IsNullOrWhiteSpace($translationKeys[$_]) })

        foreach ($key in $missingKeys) {
            if ($WarningAsError) {
                Write-Failure "Missing key in $($translation.Locale): $key"
                $totalErrors++
            } else {
                Write-Warn "Missing translation key in $($translation.Locale): $key"
                $totalWarnings++
            }
        }

        foreach ($key in $extraKeys) {
            Write-Failure "Extra key in $($translation.Locale) (not in baseline): $key"
            $totalErrors++
        }

        foreach ($key in $emptyValues) {
            if ($WarningAsError) {
                Write-Failure "Empty translation value in $($translation.Locale): $key"
                $totalErrors++
            } else {
                Write-Warn "Empty translation value in $($translation.Locale): $key"
                $totalWarnings++
            }
        }

        if ($missingKeys.Count -eq 0 -and $extraKeys.Count -eq 0 -and $emptyValues.Count -eq 0) {
            Write-Success "All $($baselineKeys.Count) keys present and translated."
        }
    }

    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    if ($totalErrors -gt 0) {
        Write-Host "  Resource validation FAILED: $totalErrors error(s), $totalWarnings warning(s)" -ForegroundColor Red
        Write-Host '========================================' -ForegroundColor Cyan
        exit 1
    }
    Write-Host "  Resource validation passed ($totalWarnings warning(s))" -ForegroundColor Green
    Write-Host '========================================' -ForegroundColor Cyan
    exit 0
}
finally {
    Pop-Location
}
