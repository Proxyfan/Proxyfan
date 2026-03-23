<#
.SYNOPSIS
    Generates API reference documentation using DefaultDocumentation CLI.

.DESCRIPTION
    Thin wrapper around the DefaultDocumentation CLI tool. Reads compiled assemblies
    and their XML documentation files produced by the build, then writes Markdown
    pages to docs/api/.

.PARAMETER Directory
    Repository root directory. Defaults to the current directory.

.PARAMETER OutputDirectory
    Output directory for generated Markdown, relative to Directory. Defaults to 'docs\api'.

.PARAMETER Configuration
    MSBuild configuration to read binaries from (Debug or Release). Defaults to 'Debug'.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Directory = ".",

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = "docs\api",

    [Parameter(Mandatory = $false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path -LiteralPath $Directory
$OutDir   = Join-Path $RepoRoot $OutputDirectory

# Clear output directory
if (Test-Path -LiteralPath $OutDir) {
    Remove-Item -LiteralPath $OutDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutDir | Out-Null

# Collect XML documentation files from built assemblies, excluding ref/ subdirs and test projects.
# Deduplicate by assembly name: pick the copy from the assembly's own project output directory
# (deepest path in src/ with the matching assembly name).
$XmlFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.xml" -File |
    Where-Object {
        $_.FullName -notmatch '\\ref\\' -and
        $_.FullName -match "\\bin\\$([regex]::Escape($Configuration))\\"
    } |
    Group-Object { $_.BaseName } |
    ForEach-Object {
        # Prefer the copy that lives in its own project's bin directory:
        # the path where the parent of bin/ matches the assembly name (case-insensitive).
        $preferred = $_.Group | Where-Object {
            $projectDir = Split-Path (Split-Path (Split-Path $_.FullName -Parent) -Parent) -Leaf
            $projectDir -like $_.BaseName
        } | Select-Object -First 1
        if ($preferred) { $preferred } else { $_.Group | Select-Object -First 1 }
    }

$Processed = 0
$Failed    = 0
$TempBase  = Join-Path ([IO.Path]::GetTempPath()) "proxyfan_docs_$([System.Diagnostics.Process]::GetCurrentProcess().Id)"

try {
    foreach ($Xml in $XmlFiles) {
        $Dll = [IO.Path]::ChangeExtension($Xml.FullName, '.dll')
        if (-not (Test-Path -LiteralPath $Dll)) {
            continue
        }

        # defaultdocumentation clears its output directory on each run, so use a per-assembly temp dir
        $TempDir = Join-Path $TempBase $Xml.BaseName
        New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

        $Output   = dotnet tool run defaultdocumentation -- `
            --AssemblyFilePath $Dll `
            --DocumentationFilePath $Xml.FullName `
            --OutputDirectoryPath $TempDir `
            --FileNameFactory FullName `
            --GeneratedPages Namespaces,Types `
            --GeneratedAccessModifiers Api `
            2>&1
        $ExitCode = $LASTEXITCODE

        if ($ExitCode -eq 0) {
            # Merge files into the shared output directory (overwrite on collision)
            Get-ChildItem -Path $TempDir -File | ForEach-Object {
                Copy-Item -Path $_.FullName -Destination $OutDir -Force
            }
            $Processed++
        } else {
            Write-Host "  [WARN] Failed to document $($Xml.Name): $(($Output | Select-Object -First 3) -join ' | ')" -ForegroundColor Yellow
            $Failed++
        }
    }
} finally {
    if (Test-Path $TempBase) {
        Remove-Item -Path $TempBase -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Remove compiler-generated types (source generators, lambda caches, etc.)
# These have obfuscated names containing patterns like .<G>$, .<M>$, or <>
$CompilerGenerated = Get-ChildItem -Path $OutDir -File -Filter "*.md" |
    Where-Object { $_.BaseName -match '\._[A-Z]_\$|<>' }
$RemovedCount = 0
$RemovedNames = @()
foreach ($File in $CompilerGenerated) {
    $RemovedNames += [regex]::Escape($File.Name)
    Remove-Item -LiteralPath $File.FullName -Force
    $RemovedCount++
}

# Scrub references to removed compiler-generated types from remaining Markdown files
if ($RemovedNames.Count -gt 0) {
    $LinkPattern = "^\|.*($($RemovedNames -join '|')).*\|.*$"
    Get-ChildItem -Path $OutDir -File -Filter "*.md" | ForEach-Object {
        $Content  = Get-Content -LiteralPath $_.FullName -Raw
        $Cleaned  = $Content -replace "(?m)$LinkPattern`r?`n", ''
        if ($Cleaned -ne $Content) {
            Set-Content -LiteralPath $_.FullName -Value $Cleaned -NoNewline
        }
    }
}

Write-Host "Output: $OutDir"
Write-Host "Documented: $Processed assemblies ($Failed failed)"
if ($RemovedCount -gt 0) {
    Write-Host "Filtered: $RemovedCount compiler-generated type(s)"
}

# Prepend coverage note to the index file
$IndexPath = Join-Path $OutDir "index.md"
if (Test-Path -LiteralPath $IndexPath) {
    $IndexContent = Get-Content -LiteralPath $IndexPath -Raw
    $CoverageNote = @"
> **Coverage Note:** This API reference is auto-generated from XML documentation comments by the
> build script. It documents assemblies under ``src/`` that have public types with XML docs.
> Projects that are scaffolding-only (no public types yet) will appear here automatically once
> public APIs are added and the solution is rebuilt.


"@
    Set-Content -LiteralPath $IndexPath -Value ($CoverageNote + $IndexContent) -NoNewline
}

if ($Processed -eq 0 -and $Failed -eq 0) {
    Write-Host "  [WARN] No XML documentation files found under src/ for configuration '$Configuration'" -ForegroundColor Yellow
}

exit 0
