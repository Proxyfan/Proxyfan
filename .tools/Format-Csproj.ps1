#Requires -Version 7

<#
.SYNOPSIS
    Normalises the XML formatting of one or more .csproj files in the
    Proxyfan repository.

.DESCRIPTION
    Hand edits, MSBuild edits, and pasted snippets routinely leave .csproj
    files with inconsistent indentation, trailing whitespace, double blank
    lines, or missing blank lines between item groups. This script rewrites
    each matching .csproj with a single, deterministic layout:

      * 4-space indent, no tabs.
      * Trailing whitespace stripped.
      * Exactly one blank line between top-level child elements.
      * Single trailing newline (LF or CRLF preserved from the source).
      * UTF-8 without BOM.
      * No XML declaration (matches the existing repo convention).

    XML comments are preserved by default. Pass `-StripComments` to drop
    them — only do this when you know the project's authors do not rely on
    inline notes (`<!-- ... -->`) to document non-obvious choices.

    Exit codes:
        0 — All processed files succeeded.
        1 — At least one file failed to parse or write.

.PARAMETER Path
    One or more file or directory paths to process. Directories are searched
    recursively for *.csproj. Defaults to the repository root (every csproj).

.PARAMETER StripComments
    Remove XML comment nodes before writing the file out.

.PARAMETER CheckOnly
    Do not modify files. Exit non-zero if any file would have changed.
    Useful for a pre-commit hook or a CI lint job.

.PARAMETER Help
    Show usage and exit.

.EXAMPLE
    .\.tools\Format-Csproj.ps1
    Normalise every .csproj under the repository root.

.EXAMPLE
    .\.tools\Format-Csproj.ps1 -Path src/Domain.Proxy/Domain.Proxy.csproj
    Normalise one specific file.

.EXAMPLE
    .\.tools\Format-Csproj.ps1 -CheckOnly
    Exit non-zero if any .csproj is not already formatted; do not write.
#>

[CmdletBinding()]
param(
    [string[]] $Path,
    [switch]   $StripComments,
    [switch]   $CheckOnly,
    [switch]   $Help
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir

Import-Module "$ScriptDir/PowerShell/Modules/Output.psm1" -Force

function Show-Help {
    Write-Host ''
    Write-Host 'Proxyfan .csproj Formatter' -ForegroundColor Cyan
    Write-Host '==========================' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Usage: .\.tools\Format-Csproj.ps1 [-Path <list>] [-StripComments] [-CheckOnly]'
    Write-Host ''
    Write-Host 'Options:'
    Write-Host '  -Path <list>     File and/or directory paths (directories scanned recursively for *.csproj).'
    Write-Host '                   Defaults to the repository root.'
    Write-Host '  -StripComments   Remove <!-- ... --> comment nodes (off by default; comments often document intent).'
    Write-Host '  -CheckOnly       Do not write; exit non-zero if any file would have changed.'
    Write-Host '  -Help            Show this help'
    Write-Host ''
}

if ($Help) { Show-Help; exit 0 }

if (-not $Path -or $Path.Count -eq 0) {
    $Path = @($RepoRoot)
}

# ─── Discovery ────────────────────────────────────────────────────────────────

$Files = [System.Collections.Generic.List[string]]::new()
foreach ($p in $Path) {
    if (-not (Test-Path -LiteralPath $p)) {
        Write-Failure "Path not found: $p"
        exit 1
    }
    $item = Get-Item -LiteralPath $p
    if ($item.PSIsContainer) {
        $matches = Get-ChildItem -LiteralPath $p -Recurse -Filter '*.csproj' -File -ErrorAction SilentlyContinue
        foreach ($m in $matches) { [void]$Files.Add($m.FullName) }
    } elseif ($item.Extension -ieq '.csproj') {
        [void]$Files.Add($item.FullName)
    } else {
        Write-Warn "Skipping non-csproj path: $p"
    }
}

if ($Files.Count -eq 0) {
    Write-Warn 'No .csproj files matched.'
    exit 0
}

# ─── Formatting helpers ───────────────────────────────────────────────────────

function Remove-XmlComments {
    param([System.Xml.XmlNode] $Node)

    $remove = [System.Collections.Generic.List[System.Xml.XmlNode]]::new()
    foreach ($child in $Node.ChildNodes) {
        if ($child -is [System.Xml.XmlComment]) {
            [void]$remove.Add($child)
        } else {
            Remove-XmlComments -Node $child
        }
    }
    foreach ($c in $remove) { [void]$Node.RemoveChild($c) }
}

function Get-LineSeparator {
    param([string] $RawContent)
    if ($RawContent -match "`r`n") { return "`r`n" }
    return "`n"
}

function Format-CsprojContent {
    param([string] $Source, [bool] $Strip)

    $doc = [System.Xml.XmlDocument]::new()
    $doc.PreserveWhitespace = $false
    $doc.LoadXml($Source)
    if ($Strip) { Remove-XmlComments -Node $doc }

    $lineSep = Get-LineSeparator -RawContent $Source

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent              = $true
    $settings.IndentChars         = '    '
    $settings.NewLineChars        = $lineSep
    $settings.OmitXmlDeclaration  = $true
    $settings.Encoding            = [System.Text.UTF8Encoding]::new($false)
    $settings.NewLineHandling     = [System.Xml.NewLineHandling]::Replace

    $sb = [System.Text.StringBuilder]::new()
    $writer = [System.Xml.XmlWriter]::Create($sb, $settings)
    try {
        $doc.Save($writer)
    } finally {
        $writer.Close()
    }

    $rendered = $sb.ToString()
    $lines = $rendered -split [regex]::Escape($lineSep)

    # Insert blank lines after the opening <Project ...> tag and before each
    # top-level closing tag at the four-space indent.
    $result = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = ($lines[$i] -replace '[ \t]+$', '')
        [void]$result.Add($line)
        if ($line -match '^<Project(\s|>)') {
            [void]$result.Add('')
        } elseif ($line -match '^    </[A-Za-z]') {
            [void]$result.Add('')
        }
    }

    # Collapse runs of >1 blank line and drop trailing whitespace, then add a single trailing newline.
    $body = (($result -join $lineSep)).TrimEnd() + $lineSep
    $body = ($body -replace "($([regex]::Escape($lineSep))){3,}", ($lineSep * 2))
    return $body
}

# ─── Main ─────────────────────────────────────────────────────────────────────

Write-Banner ('Proxyfan .csproj Formatter ({0} file{1})' -f $Files.Count, ($(if ($Files.Count -eq 1) { '' } else { 's' })))

$rewriteCount  = 0
$alreadyClean  = 0
$wouldChange   = 0
$errors        = 0

foreach ($file in $Files) {
    $rel = $file.Replace($RepoRoot, '').TrimStart('\','/')
    try {
        $original  = Get-Content -LiteralPath $file -Raw
        $formatted = Format-CsprojContent -Source $original -Strip:$StripComments

        if ($original -eq $formatted) {
            $alreadyClean++
            Write-Info ("clean   $rel")
            continue
        }

        if ($CheckOnly) {
            $wouldChange++
            Write-Warn ("changed $rel")
            continue
        }

        $utf8 = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($file, $formatted, $utf8)
        $rewriteCount++
        Write-Success ("rewrote $rel")
    } catch {
        $errors++
        Write-Failure ("error   $rel — $($_.Exception.Message)")
    }
}

Write-Host ''
if ($CheckOnly) {
    if ($wouldChange -gt 0 -or $errors -gt 0) {
        Write-Failure ("{0} file(s) need reformatting, {1} clean, {2} error(s)." -f $wouldChange, $alreadyClean, $errors)
        exit 1
    }
    Write-Success ("All {0} file(s) are already formatted." -f $alreadyClean)
    exit 0
}

if ($errors -gt 0) {
    Write-Failure ("Done with errors. rewrote={0}, clean={1}, errors={2}" -f $rewriteCount, $alreadyClean, $errors)
    exit 1
}

Write-Success ("Done. rewrote={0}, clean={1}" -f $rewriteCount, $alreadyClean)
exit 0
