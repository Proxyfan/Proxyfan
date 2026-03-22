# AGENTS.md

This file provides guidance to coding agents (Claude Code, Codex, Jules, OpenCode, etc.) when working with code in this repository.

## Development Environment

This repository's **entire development lifecycle runs on Windows** — CI pipelines, builds, tests, and tooling are all Windows-only. There is no Linux or macOS support in the pipeline.

**PowerShell 7 (`pwsh`) is the only approved scripting language for this repo.** All scripts, automation, and tooling must be written in PowerShell 7. Do **not** use bash scripts, Python scripts, batch files (`.cmd`/`.bat`), JavaScript/Node.js scripts, or any other scripting language — even for one-off tasks. Do **not** use `powershell.exe` (legacy Windows PowerShell) — it fails to parse several project scripts due to Unicode characters.

When a Bash tool must be used (e.g., for git commands or shell built-ins), invoke PowerShell through it:

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Some-Script.ps1
```

## Build Script

**Always use `.tools/Invoke-Build.ps1` to build the codebase.** This script builds the solution and regenerates `docs/api/` which agents should read instead of scanning source files directly.

```powershell
# Standard build (Debug, regenerates API reference)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build + run tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Skip package restore (incremental, when dependencies have not changed)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore
```

Do **not** run `dotnet build` directly — `docs/api/` will not be updated.

| Script | Purpose |
|--------|---------|
| `.tools/Invoke-Build.ps1` | **Canonical build script** — see above. Accepts `-RunTests`, `-Configuration`, `-SkipRestore`. |
| `.tools/Run-Tests.ps1` | **Canonical test script** — see below. Accepts `-Configuration`, `-NoBuild`. |
| `.tools/Invoke-Cleanup.ps1` | **Canonical cleanup script** — see below. Applies consistent formatting and code style. |
| `.tools/Initialize-Repository.ps1` | First-time dev machine setup. Accepts `-SkipWorkloads`, `-SkipTools`, `-RunTests`. |

## Test Script

**Always use `.tools/Run-Tests.ps1` to run tests.** Do **not** use `dotnet test` directly — it bypasses the script's structured output, failure summaries, and consistent runner flags.

```powershell
# Run full test suite (Debug, includes build)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1

# Skip rebuild when code is already built
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -NoBuild

# Run under Release configuration
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -Configuration Release
```

To run a **single test**, use `dotnet run --project tests/<Project> -- --filter "ClassName.MethodName"` directly.

## Code Cleanup

Run `.tools/Invoke-Cleanup.ps1` on demand to apply consistent formatting and code style. Do **not** run it automatically before commits or when completing tasks — only run it when explicitly asked.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Cleanup.ps1
```

## Rules for Agents

- **CRITICAL**: **Always use `docs/api/` as the primary source of truth for any public API.**
  Before reading any source file under `src/` to understand a type's public interface, check
  `docs/api/` first. The index at `docs/api/index.md` lists every documented namespace;
  individual type pages live at `docs/api/{Namespace}.{TypeName}.md` (FileNameFactory FullName).
  Read a source file only when you need implementation details the API reference cannot provide
  (e.g., method bodies, private fields, algorithmic internals).

- **CRITICAL**: **Always run `.tools/Invoke-Build.ps1` before committing when the public API has changed.**
  A "public API change" is any addition, removal, or modification of a `public` or `protected`
  member in `src/`. Running the build script keeps `docs/api/` in sync.

- **CRITICAL**: **Never suppress analyzer diagnostics with `#pragma warning disable`.** If a diagnostic fires, fix the root cause — refactor the code, adjust the design, or extend the analyzer's allow-list if the rule is genuinely inapplicable. `#pragma` suppressions are forbidden in all files under `src/` and `tests/`, with no exceptions.

## Project Context

Proxyfan is an **HTTP debugging proxy** for inspecting, capturing, and modifying network traffic in real time. It is built on .NET 10 with Avalonia for cross-platform UI and CommunityToolkit.Mvvm for the MVVM layer.

## Key Patterns

- **MVVM**: CommunityToolkit.Mvvm with `[ObservableProperty]`, `[RelayCommand]`
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection` via `IHostBuilder`
- **Options Pattern**: Strongly-typed options bound via `Microsoft.Extensions.Options`
- **Central Package Management**: All NuGet versions are declared in `Directory.Packages.props` — never specify versions in individual `.csproj` files

## Roslyn Linting Rules (enforced as errors)

- **IDE0022**: Block body required for methods (no `=>` arrow methods with parameters; properties are fine)
- **IDE0045**: Use `if/else` instead of ternary for assignments
- **IDE0046**: Use `if/else` instead of ternary for returns
- **S121**: All `if`/`else` branches must use curly braces (no inline single-statement bodies)

## Test Suite

All test projects use **TUnit** (not xUnit or NUnit) with the **Microsoft.Testing.Platform** runner.
Test projects live under `tests/`, one per source project.

**File naming**: `{ClassUnderTest}Tests.cs`
**Class naming**: `{ClassUnderTest}Tests` — the test class name **must** match the class under test. Never name a test class after a topic or feature. Multiple test classes for the same type follow the pattern `{ClassUnderTest}{Qualifier}Tests`.
**Method naming**: `{Method}_{Scenario}_{ExpectedResult}`

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

- Use `[NotInParallel]` on classes that mutate shared state
- Use hand-written stubs in `Stubs/` subdirectories — mocking frameworks (`Moq`, `NSubstitute`, `FakeItEasy`, etc.) are forbidden
- Common assertions: `IsEqualTo`, `IsTrue`, `IsFalse`, `IsNotNull`, `IsNull`, `Count().IsEqualTo(n)`, `Throws<T>`, `IsSameReferenceAs`
- Architecture conformance tests use **ArchUnitNET** (`TngTech.ArchUnitNET.TUnit`)

## Key Dependencies

| Package | Use |
|---------|-----|
| Avalonia 11.3.11 | Cross-platform UI framework |
| CommunityToolkit.Mvvm 8.4.0 | MVVM source generators |
| TUnit 1.12.93 | Testing framework (not xUnit/NUnit) |
| TngTech.ArchUnitNET 0.13.3 | Architecture conformance tests |
| SonarAnalyzer.CSharp 10.19.0 | Static analysis (enforced as errors) |
