# Contributing to Proxyfan

Thanks for your interest in contributing! This document describes the workflow and the
conventions that contributions are expected to follow.

## Code of conduct

Participation in this project is governed by the
[Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to abide by it.

## Development environment

Proxyfan is **Windows-only**. CI, builds, tests, and tooling all target Windows.

### Prerequisites

- [.NET 10 SDK](https://dot.net/download) — version pinned in `global.json`
- [PowerShell 7](https://github.com/PowerShell/PowerShell) (`pwsh`)
- A GitHub personal access token with `read:packages` for the `Automaticks` organization
  (the project consumes private analyzer packages from GitHub Packages). Set it as
  `GITHUB_TOKEN` in your environment before restoring.

### First-time setup

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-Repository.ps1
```

This installs the required workloads, restores NuGet packages, and builds the solution.

## Build, test, lint

All canonical operations go through the `.tools/` scripts. Do **not** invoke
`dotnet build` or `dotnet test` directly except for narrowly targeted runs.

| Script | Purpose |
| --- | --- |
| `.tools/Invoke-Build.ps1` | Canonical build (regenerates `docs/api/` when present) |
| `.tools/Run-Tests.ps1` | Canonical test runner |
| `.tools/Invoke-Cleanup.ps1` | JetBrains code cleanup on changed `.cs` files (on-demand) |
| `.tools/Build-Installer.ps1` | Portable ZIP build for releases |
| `.tools/Test-ResourceKeys.ps1` | Validates translation `.resx` files |

```powershell
# Build
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build + tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Single test
dotnet run --project tests/<Project> -- --treenode-filter "/*/*/MyTests/*"
```

## Coding conventions

Proxyfan builds with `TreatWarningsAsErrors=true` and enforces the following:

- **No `#pragma warning disable`** in `src/` or `tests/`. Fix the root cause.
- **All public and protected members** must have XML doc comments (`CS1591`).
- **No LINQ** in domain or framework code (analyzer `ATXLQ002`). Use explicit loops.
- **Hand-written stubs** in `Stubs/` subdirectories — mocking frameworks (`Moq`,
  `NSubstitute`, etc.) are forbidden.
- **TUnit** with **Microsoft.Testing.Platform** is the only test framework.
- **No `git commit` / `git push` from automation.** The human developer handles all
  staging, committing, and pushing.

### Style rules

- **File formatting** — UTF-8 BOM, CRLF line endings, 4-space indentation (2 for JSON)
- **Naming** — boolean-returning methods start with `Can` or `Has`; boolean properties
  start with `Is` or `Allow`
- **Member ordering** — events → constants → fields → properties → indexers →
  constructors → implementations → methods → nested types; within each group, public
  before protected before private, static before instance, then alphabetical
- **Using order** — case-insensitive alphabetical
- **No inline `new`** — assign to a local variable first
- **No arrow-body methods with parameters** (`IDE0022`); properties may use arrow bodies
- **`if/else` over ternary for assignments and returns** (`IDE0045`, `IDE0046`)
- **Curly braces on every `if`/`else` branch** (`S121`)

## Test conventions

| Convention | Rule |
| --- | --- |
| File name | `{ClassUnderTest}Tests.cs` |
| Class name | `{ClassUnderTest}Tests` or `{ClassUnderTest}{Qualifier}Tests` |
| Method name | `{Method}_{Scenario}_{ExpectedResult}` (exactly three underscore parts) |
| Parallelism | `[NotInParallel]` on classes that mutate shared state |
| Coverage | Minimum 80% line + branch per module; project currently sits at 99/97 |

## Submitting changes

1. **Fork** the repository.
2. Create a **topic branch** (`git checkout -b my-feature`).
3. Make your changes following the conventions above.
4. **Run tests locally** before opening a PR.
5. **Open a pull request** with a clear description of what changed and why.
6. **Sign off your commits** if you intend to dedicate them to the project (optional but
   appreciated).

### Commit messages

- Subject line in present tense ("Add X" not "Added X"), ≤ 72 characters.
- Body explains *why* the change is needed; the diff already shows *what* changed.
- Reference the relevant `docs/BACKLOG.md` ID where applicable (e.g., `(E11-F02-T01)`).

## Reporting bugs

Open an issue with:

- Proxyfan version (Help → About → Version)
- Windows version
- Steps to reproduce
- Expected vs actual behavior
- Relevant log excerpt from `%LOCALAPPDATA%\Proxyfan\logs\` (redact any sensitive data)

## Requesting features

Open an issue describing the use case before opening a PR for a non-trivial feature.
Most product features are tracked in [docs/BACKLOG.md](docs/BACKLOG.md); check there
first to see if the work is already planned.
