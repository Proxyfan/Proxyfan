# Copilot Instructions — Proxyfan

Proxyfan is an HTTP debugging proxy for inspecting, capturing, and modifying network traffic in real time on Windows, built with .NET.

## Git Operations

**Never run `git commit` or `git push`.** Staging, committing, and pushing is strictly reserved for the human developer. Prepare your changes and stop — do not commit or push under any circumstances, even when asked to "finish" or "complete" a task.

## Development Environment

All scripts must be written in **PowerShell 7 (`pwsh`)** — never bash, batch files (`.cmd`/`.bat`), or legacy `powershell.exe`.

## Build, Test, and Lint

> All `.tools/` scripts are the canonical entry points. Only invoke `dotnet build`, `dotnet test`, or `dotnet restore` for very targeted operations.

| Script | Purpose |
|---|---|
| `.tools/Invoke-Build.ps1` | Canonical build — restores, builds, regenerates `docs/api/`. Accepts `-RunTests`, `-SkipRestore`, `-Configuration`. |
| `.tools/Run-Tests.ps1` | Canonical test runner. Accepts `-NoBuild`, `-Configuration`. |
| `.tools/Invoke-Cleanup.ps1` | JetBrains code cleanup on changed `.cs` files. **Run only when explicitly asked.** |
| `.tools/Initialize-Repository.ps1` | First-time dev machine setup (workloads, packages, tools, build). Accepts `-SkipWorkloads`, `-SkipTools`, `-RunTests`. |

```powershell
# First-time setup
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-Repository.ps1

# Standard Debug build (+ regenerates docs/api/)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build + tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Incremental build (packages unchanged)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore

# Full test suite
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1

# Tests only (code already built)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -NoBuild

# Code cleanup (on-demand only)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Cleanup.ps1
```

To run a **single test**, use `dotnet run --project tests/<Project> -- --filter "ClassName.MethodName"` directly (bypassing the test script is acceptable for targeted runs).

## Architecture

Package versions are managed centrally in `Directory.Packages.props` — never specify versions in individual `.csproj` files.

The `Environment` MSBuild property (default: `Development`) defines a compile-time constant `ENVIRONMENT_<VALUE>` available across all projects.

All public and protected members **must** have XML documentation comments — `CS1591` is treated as an error.

## Key Conventions

### Analyzers and Code Style

- **`TreatWarningsAsErrors = true`** — all analyzer diagnostics are errors; fix root causes rather than suppressing them.
- **Never use `#pragma warning disable`** in files under `src/` or `tests/`.
- **SonarAnalyzer.CSharp** runs on every build as a first-class linting pass (error severity).
- Enforced style rules (will fail the build):
  - `IDE0022`: Block body required for methods — no `=>` arrow methods with parameters (properties are fine)
  - `IDE0045` / `IDE0046`: Use `if/else` instead of ternary for assignments and returns
  - `S121`: All `if`/`else` branches must use curly braces

### Testing

All test projects use **TUnit** with the **Microsoft.Testing.Platform** runner (configured in `global.json`). **xUnit and NUnit are not used.**

| Convention | Rule |
|---|---|
| File | `{ClassUnderTest}Tests.cs` |
| Class | `{ClassUnderTest}Tests` — named after the type under test, never a feature or topic |
| Method | `{Method}_{Scenario}_{ExpectedResult}` |
| Stubs | Hand-written stubs in `Stubs/` subdirectories — mocking frameworks (`Moq`, `NSubstitute`, etc.) are forbidden |
| Parallelism | `[NotInParallel]` on classes that mutate shared state |

```csharp
[Test]
public async Task Parse_ValidInput_ReturnsExpected()
{
    var result = MyParser.Parse("input");
    await Assert.That(result).IsEqualTo(expected);
}

// Parameterized:
[Test]
[Arguments("a", 1)]
public async Task Parse_Variant_ReturnsExpected(string input, int expected)
{
    await Assert.That(MyParser.Parse(input)).IsEqualTo(expected);
}
```

Architecture conformance tests use **ArchUnitNET** (`TngTech.ArchUnitNET.TUnit`).

### File Formatting

- **Charset**: UTF-8 BOM (`charset = utf-8-bom`)
- **Line endings**: CRLF
- **Indentation**: 4 spaces (2 spaces for JSON files)
- Max one blank line between code blocks in C# files

### API Reference

After any public API change (addition, removal, or modification of a `public`/`protected` member), run `.tools/Invoke-Build.ps1` to regenerate `docs/api/`. Prefer reading `docs/api/` over scanning source files when exploring public interfaces.
