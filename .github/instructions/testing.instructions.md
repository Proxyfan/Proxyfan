---
applyTo: "tests/**/*.cs"
---

# Test conventions

Proxyfan's test suite uses **TUnit** on the **Microsoft.Testing.Platform** runner
(see `global.json`'s `"test": { "runner": "Microsoft.Testing.Platform" }`).
xUnit, NUnit, and MSTest are **not** used. Run the suite via
`.tools/Run-Tests.ps1`; single-test runs go through `dotnet run --project` with
the `--treenode-filter` flag.

## Project layout

- One test project per source project, mirroring the name (`Domain.Rules` →
  `Domain.Rules.Tests`).
- Hand-written stubs live under `Stubs/`.
- Reusable factories and assertion helpers live under `Helpers/`.
- Static fixtures live under `TestData/`.
- Architectural-conformance tests live under `Architecture/` and use
  `TngTech.ArchUnitNET.TUnit`.
- End-to-end UI tests set `<IsEndToEndTestProject>true</IsEndToEndTestProject>`
  in their `.csproj`. `Run-Tests.ps1` excludes them unless `-IncludeEndToEnd` is
  passed; CI never runs them.

## Naming

| Concern | Rule |
|---|---|
| File | `{ClassUnderTest}Tests.cs` |
| Class | `{ClassUnderTest}Tests` — or `{ClassUnderTest}{Qualifier}Tests` (e.g. `RuleEngineMutationTests`) when a single class is exercised from multiple angles |
| Method | `{Method}_{Scenario}_{ExpectedResult}` — exactly three underscore-separated PascalCase parts (`ATXTST003`) |

`{Method}` is the name of the production method or the constructor (`Ctor` is
acceptable for constructor scenarios). `{Scenario}` describes the input shape
or precondition. `{ExpectedResult}` describes the observable outcome.

```csharp
[Test]
public async Task EvaluateRequest_BlockListMatch_ShortCircuits()
{
    var engine = new RuleEngine(registry: BlockListOnly("evil.example"));
    var actions = engine.EvaluateRequest(RequestFor("https://evil.example/api"));

    await Assert.That(actions).HasCount().EqualTo(1);
    await Assert.That(actions[0]).IsTypeOf<RequestPipelineAction.Block>();
}
```

## Mocking is forbidden (`ATXTST001`)

`Moq`, `NSubstitute`, `FakeItEasy`, `AutoFixture`, and similar packages must
not appear in `Directory.Packages.props`. Use **hand-written stubs** in a
`Stubs/` subdirectory next to the test class. Stubs are minimal: implement only
the surface the test exercises, fail fast on unexpected calls
(`throw new InvalidOperationException("Stub: unexpected call to …")`),
and never re-implement business logic.

## Parameterised tests

Use TUnit's `[Arguments]` attribute. Each argument tuple becomes a separate
test instance:

```csharp
[Test]
[Arguments("GET", true)]
[Arguments("POST", true)]
[Arguments("CONNECT", false)]
public async Task IsForwardable_KnownMethod_ReturnsExpected(string method, bool expected)
{
    await Assert.That(HypertextTransferProtocolMethods.IsForwardable(method)).IsEqualTo(expected);
}
```

## Parallelism and shared state

- TUnit runs test classes in parallel by default.
- Classes that mutate process-global state (environment variables, the working
  directory, registered DI services, file-system state outside `Path.GetTempPath`)
  must be decorated with `[NotInParallel]`. Pair the attribute with a
  per-class reset in `[Before(Test)]` and `[After(Test)]`.
- Group related tests that share a fixture into the same `[NotInParallel(group)]`
  so they serialise against each other but stay parallel with unrelated tests.

## Asynchrony

- All test methods return `Task` and `await` the assertion expression.
- `Task.Delay` is forbidden (`ATXTST004`); use deterministic event waits, e.g. a
  `TaskCompletionSource` flipped from a stubbed callback.
- `Thread.Sleep` is forbidden — same reasoning.
- `CancellationToken` parameters in async tests use
  `TestContext.Current.CancellationToken` (TUnit-managed) when available.

## Assertions

Preferred patterns (TUnit fluent API, always `await`ed):

```csharp
await Assert.That(value).IsEqualTo(expected);
await Assert.That(text).Contains("expected substring");
await Assert.That(collection).HasCount().EqualTo(3);
await Assert.That(reference).IsSameReferenceAs(other);
await Assert.That(result.IsSuccess).IsTrue();
await Assert.That(result.Error).IsNotNull();
await Assert.That(result.Error!.Code).IsEqualTo("RULE_BREAKPOINT_TIMED_OUT");
```

For thrown exceptions:

```csharp
await Assert.That(() => parser.Parse("bad")).Throws<FormatException>();
await Assert.That(async () => await store.SaveAsync(flow)).Throws<SessionWriteException>();
await Assert.That(() => parser.Parse("good")).ThrowsNothing();
```

## Avalonia stubs

Avalonia controls pin to the UI thread via `Dispatcher.VerifyAccess`, which
throws on the test threadpool. Test doubles for any view-side abstraction
(`IOverlayHost`, `IDialogView`, `IToolWindow`, etc.) must implement the
interface on a **plain class**, not a `Control` / `UserControl` / `Window`.

For genuine UI verification, use:

- `Client.EndToEndTests` (Avalonia.Headless + TUnit; opt-in via
  `-IncludeEndToEnd`).
- `Client.UiAutomationTests` (FlaUI + UIA3; launches a real
  `Client.Desktop.exe`).

Both are gated out of CI; never make the standard suite depend on them.

## Test data factories

Construct domain types through factory helpers (`TrafficFlowFactory.CreateValid()`,
`ContentTypeFactory.Json()`, etc.) so common shapes have one canonical builder.
A factory should expose the most opinionated default — tests then mutate only
the fields they care about.

## Architecture tests (`TngTech.ArchUnitNET`)

`Architecture/` folders contain conformance tests that assert layer dependency
rules, naming conventions, and the absence of circular project references.
When the dependency map changes, update the matching test rather than removing
or weakening the existing rule.

## Coverage

Per-module minimum: 80 % line + 80 % branch. The repository typically sits
higher; do not regress a module below 80 % without a justification recorded
in the PR description. `coverlet.collector` is wired into every test project.
