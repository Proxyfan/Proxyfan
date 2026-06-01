---
applyTo: "**/*.cs"
---

# C# coding rules

Proxyfan enforces every analyzer diagnostic as a build error
(`TreatWarningsAsErrors=true` in `Directory.Build.props`). The active analyzer
packages are:

- `SonarAnalyzer.CSharp` (Sonar rule IDs — `S121`, `S6602`, `S2971`, etc.)
- The `Automaticks.*` family (`Automaticks.CSharp.Analyzers`,
  `Automaticks.Threading.Tasks.Analyzers`, `Automaticks.Linq.Analyzers`,
  `Automaticks.Reflection.Analyzers`, `Automaticks.Extensions.Options.Analyzers`,
  `Automaticks.Diagnostics.CodeAnalysis.Analyzers`,
  `Automaticks.CommunityToolkit.Mvvm.Analyzers`,
  `Automaticks.Testing.Analyzers`) → rule IDs of the form
  `ATXCS###`, `ATXTA###`, `ATXLQ###`, `ATXRF###`, `ATXEO###`, `ATXDC###`,
  `ATXMV###`, `ATXTST###`.

## Absolute prohibitions

These never ship through any code-change automation:

| Prohibition | Reason |
|---|---|
| `#pragma warning disable` / `#pragma warning restore` | Silences analyzers — `AGENTS.md` rule. Fix the root cause. |
| `[SuppressMessage(…)]` | Same as above. The analyzer ID is right; the code is wrong. |
| Editing `<NoWarn>` to clear an active failure | A deliberate, reviewed exception is a separate PR. |
| Editing `.editorconfig` severity to demote a current failure | Severity belongs in its own change. |
| `params` keyword | `ATXCS055` forbids it across all projects. |
| Mocking frameworks (`Moq`, `NSubstitute`, `FakeItEasy`, `AutoFixture`, …) | `ATXTST001` — hand-written stubs only. |
| LINQ in production code | `s6602` is error severity; loops are required in `src/`. Tests may use LINQ. |
| Primary constructors | `IDE0290` is silenced because primary constructors are forbidden (`ATXCS037`); use an explicit constructor. |
| Inline field initialisation on classes with a static constructor | `S3963` is silenced because `ATXCS036` forbids it. |
| Static methods on non-static classes | `CA1822` / `S2325` silenced because `ATXCS011` forbids it — move static helpers to a `*static class*` partner type. |

## Style rules that **will** fail the build

The following IDs are explicitly raised to `error` in `.editorconfig` and a
violation breaks the build:

- `IDE0022` — methods with parameters must have a block body, not an arrow body.
  Properties may use arrow bodies.
- `IDE0045` — do not use the ternary operator for assignments. Use `if`/`else`.
- `IDE0046` — do not use the ternary operator for returns. Use `if`/`else`.
- `S121` — every branch of `if`, `else`, `for`, `foreach`, `while`, `do` must
  use curly braces, even single-statement bodies.
- `S6602` — `Find` / `Exists` on `List<T>` rather than LINQ `FirstOrDefault` /
  `Any`. Combined with `s3267` being silenced, the message is "use indexers
  and dedicated methods, not LINQ".
- `CS1591` — every public/protected member needs an XML doc comment.

## Style rules requiring manual fix

- **Method size** — keep methods short and focused. When a method begins to span
  more than ~50 non-blank lines, extract a helper before the next analyzer rule
  fires.
- **Cyclomatic complexity** — refactor before nesting goes deeper than three
  levels. Pull guard clauses into early returns, hoist conditions into named
  predicates.
- **Parameter count** — prefer a dedicated `*Dependencies` record (see
  `HypertextTransferProtocolProxyHandlerDependencies`,
  `TransportLayerSecurityInterceptorHandlerDependencies`) or a parameter object
  rather than long argument lists. This sidesteps the four-parameter analyzer
  cap and improves DI ergonomics.
- **Boolean naming** — boolean properties / fields begin with `Is` or `Allow`
  (case-insensitive; a leading `_` is stripped first). Boolean-returning methods
  begin with `Can` or `Has`. Both rules skip overrides such as `Equals`.
- **Member ordering** — events → constants → fields → properties → indexers →
  constructors → implementations (interface impls and `override`s) → methods →
  nested types. Within each group: public → protected → private, static →
  instance, then alphabetical (`_` sorts before letters).
- **Using ordering** — case-insensitive alphabetical, no duplicates, blank line
  between `using`s and the namespace declaration.
- **Inline `new` in arguments** — `Foo(new Bar(…))` is forbidden by `ATXCS058`.
  Assign to a named local first: `var bar = new Bar(…); Foo(bar);`. Pattern
  visible across `Result.Success(value)`, `Result.Failure(error)` etc.
- **Object / collection initialisers** — one element per line; empty `{}`
  forbidden.
- **Default parameter values** — forbidden (`ATXCS057`). Every caller passes
  every argument explicitly. Provide an overload when a default is genuinely
  appropriate.
- **No null-guard on non-nullable parameters** — the nullable annotation
  carries the contract. Use `?` if `null` is legitimate input.
- **No vague suffixes** — `Service`, `Helper`, `Util`, `Utils`, `Utilities` are
  forbidden as type-name suffixes. Use the precise vocabulary listed in
  `architecture.instructions.md`.

## File formatting

- UTF-8 with BOM (`charset = utf-8-bom`).
- CRLF line endings.
- 4-space indentation (2 spaces for `.json`).
- File-scoped namespace declarations are *not* preferred (`file_scoped:none`);
  use block-scoped namespaces consistently across the codebase.
- Maximum one blank line between two constructs.

## Disabled diagnostics

`.editorconfig` intentionally silences a handful of CA / Sonar rules because a
stricter Automaticks rule already covers them or because the diagnostic is a
false positive in this codebase. Do not re-enable them without consulting the
file's notes:

| Disabled | Reason |
|---|---|
| `CA1716` | `Shared` is a legitimate namespace despite being a VB keyword. |
| `CA1711` | `ReadOnlyMemoryStream` deliberately ends in `Stream`. |
| `CA1710` | `ConcurrentList` deliberately does not end in `Collection`. |
| `CA1707` | Test methods use underscores (`Method_Scenario_Expected`). |
| `IDE0072` | Switch expressions intentionally elide some cases. |
| `IDE0130` | Avalonia XAML temp projects produce false namespace mismatches. |
| `IDE0290` | `ATXCS037` forbids primary constructors. |
| `CA1848` | `LoggerMessage` source generator is a deferred optimisation. |
| `CA1822` / `S2325` | `ATXCS011` forbids static methods on non-static classes — move them. |
| `S3963` | `ATXCS036` requires explicit static constructors instead of inline init. |
| `S2094` | Assembly-marker classes are intentionally empty. |
| `IDE0058` | TUnit's assertion return values are intentionally unused. |
| `S3267` | LINQ-replacement nag overruled by `s6602` which already errors. |

## Generic delegate types

`Action`, `Func`, `Predicate`, `Comparison`, `Converter` are discouraged for
public APIs — define a named delegate type (or an interface) when a contract is
meaningful enough to be referenced from multiple call sites.

## Diagnostic triage

When a build surfaces an analyzer error, never silence it. The order of
operations is:

1. Re-read the diagnostic message — most analyzer rules describe the fix
   directly.
2. Check `.github/instructions/csharp-rules.instructions.md` (this file) for the
   ID.
3. If the ID is not catalogued here, search the codebase for a sibling that
   already complies — there is almost always an example two folders away.
4. If a deliberate exception is genuinely warranted, raise it as a separate PR
   that adds the ID to the project's `<NoWarn>` with a one-paragraph rationale.
   Never bundle the silencer with the failing change.
