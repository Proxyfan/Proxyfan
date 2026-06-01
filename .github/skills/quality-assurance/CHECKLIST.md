# Quality-assurance checklist

Detailed reference for the `quality-assurance` skill, covering both
whole-codebase analysis and PR-diff review.

## Project conventions (TUnit on Microsoft.Testing.Platform)

- **Runner**: `Microsoft.Testing.Platform` (set in `global.json`).
- **Framework**: TUnit. xUnit, NUnit, MSTest are not used.
- **Class naming**: `{ClassUnderTest}Tests` (or `{ClassUnderTest}{Qualifier}Tests`)
  — the class name must reference a real type, with the `Tests` suffix
  stripped (`ATXTST002`).
- **Method naming**: `{Method}_{Scenario}_{ExpectedResult}` — exactly three
  underscore-separated PascalCase parts (`ATXTST003`).
- **Parameterised tests**: `[Arguments(...)]` attribute.
- **Shared-state tests**: `[NotInParallel]`.
- **Mocking frameworks**: forbidden (`ATXTST001`). Use hand-written stubs
  under `Stubs/`.
- **`Task.Delay` / `Thread.Sleep` in tests**: forbidden (`ATXTST004`).
- **Assertions**: TUnit fluent API, always `await`ed
  (`await Assert.That(value).IsEqualTo(expected)`).

## Analysis

1. **Coverage gaps.** Identify changed public methods, decision branches,
   and error paths with no corresponding test. High-priority targets:
   - `Domain.Rules/RuleEngine.cs` — every short-circuit case.
   - `Domain.Traffic/TrafficStore.cs` — eviction at capacity, observation
     callbacks, large-body spill.
   - `Framework.Networking/HypertextTransferProtocolForwarder.cs` and
     siblings — every forwarding outcome.
   - `Framework.Networking/HypertextTransferProtocolVersion2*.cs` — HPACK
     decode/encode, frame parsing, stream-state transitions.
   - `Domain.Scripting/RoslynUserScriptCompiler.cs` — compilation errors,
     warnings, timeout, OOM.
   - `Domain.Certificates/CertificateAuthority.cs` and `LeafCertificateCache.cs`
     — generation, caching, invalidation.

2. **Naming.** Confirm `ATXTST002` (class matches type under test) and
   `ATXTST003` (three-part method name).

3. **Assertion quality.** Flag:
   - Tests with no assertion.
   - Assertions that always pass.
   - Weak assertions (`IsNotNull` where `IsEqualTo` would be more
     meaningful).
   - Assertions that test implementation detail instead of observable
     behaviour.

4. **Flakiness.** Detect:
   - `Task.Delay` / `Thread.Sleep` (banned, `ATXTST004`).
   - Tests that depend on execution order.
   - Mutations of process-global state without `[NotInParallel]`.
   - Non-deterministic data (random values, `DateTime.UtcNow`) used in
     assertions.

5. **Stub quality.** Stubs in `Stubs/` must be minimal:
   - Implement only the surface the test exercises.
   - Throw `InvalidOperationException` on unexpected calls.
   - Never re-implement business logic.
   - Flag mocking-framework usage (`Moq`, `NSubstitute`, …) — banned
     (`ATXTST001`).

6. **Setup and teardown.** Verify `[Before(Test)]` / `[After(Test)]` for
   per-test setup, and `[Before(Class)]` / `[After(Class)]` for per-class
   setup. Confirm reset for shared state in classes marked `[NotInParallel]`.

7. **Integration coverage.** Flag end-to-end flows tested only in
   isolation. The integration seams worth covering:
   - Accept → dispatch → handler chain (verifying protocol detection).
   - TLS interception → leaf cert mint → cache hit (verifying cache
     behaviour and ALPN mirroring).
   - Rule engine → request rewrite → response rewrite cycle.
   - Traffic store → flow created → flow completed event cascade.
   - HAR import → in-memory hydration → HAR export round-trip.

8. **End-to-end coverage.** `Client.EndToEndTests` (Avalonia headless) and
   `Client.UiAutomationTests` (FlaUI) are the surfaces. Both are opt-in via
   `Run-Tests.ps1 -IncludeEndToEnd` and are excluded from CI. New UI
   surfaces that affect the user-facing flow should add at least one E2E
   test.

9. **Architecture tests.** `tests/*.Tests/Architecture/` enforces layer
   rules via `TngTech.ArchUnitNET.TUnit`. When a new project, namespace, or
   dependency direction is introduced, update the architecture tests to
   capture the rule rather than weakening or removing the existing rule.

10. **Test focus.** Flag tests that exercise multiple distinct behaviours
    in one method — split into one test per behaviour.

11. **Parameterised coverage.** Suggest `[Arguments]` when one test method
    would cover multiple equivalent scenarios without duplicating logic.

## Reading the test runner output

`Run-Tests.ps1` aggregates per-project test results. On failure it prints
the failed tests, the failed project list, and the summary lines. When
diagnosing a flake, prefer:

- A single-test run via
  `dotnet run --project tests/<Project> -- --treenode-filter "/*/*/<Class>/<Method>"`.
- Iterating with `Invoke-Build.ps1 -SkipRestore -RunTests` to keep the
  build incremental.

## Forbidden silencers in proposed fixes

Never recommend `[Skip]`, `[Explicit]`, deletion of a failing test,
commenting out an assertion, or weakening an assertion. Failing tests
indicate either a production bug or a stale expectation — both have
legitimate fixes.
