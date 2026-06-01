# Scripting-sandbox checklist

Detailed reference for the `scripting-sandbox` skill. Read
`.github/instructions/scripting-sandbox.instructions.md` first — it is the
canonical source for the sandbox contract.

## Surfaces

- `Domain.Scripting/RoslynUserScriptCompiler.cs` and
  `RoslynUserScriptCompilerHelpers.cs` — compilation pipeline and reference
  allow-list.
- `Domain.Scripting/RoslynUserScript.cs` — compiled script handle.
- `Domain.Scripting/IUserScript.cs`, `IUserScriptCompiler.cs`,
  `IScriptingHandler.cs`, `UserScriptingHandler.cs`,
  `UserScriptingHandlerHelpers.cs` — abstractions and orchestration.
- `Domain.Scripting/RequestScriptGlobals.cs`,
  `ResponseScriptGlobals.cs` — globals exposed to the script body.
- `Domain.Scripting/ScriptableRequest.cs`, `ScriptableResponse.cs`,
  `ScriptableHeaders.cs`, `ScriptableProjector.cs` — the projection layer
  that prevents direct mutation of the underlying `HypertextTransferProtocolRequestData`
  / `ResponseData`.
- `Domain.Scripting/MutableScriptingConfiguration.cs`,
  `MutableScriptingConfigurationChanged.cs` — hot-reloadable script set.
- `Domain.Scripting/ScriptCompilationResult.cs`, `ScriptCompilationResults.cs`,
  `ScriptDiagnostic.cs`, `ScriptDiagnosticSeverity.cs`, `ScriptError.cs`.

## Analysis

1. **Compilation pipeline.** Confirm:
   - Each `OnRequest` / `OnResponse` body compiles into a `RoslynUserScript`
     with strict error-on-warning suppression where appropriate.
   - Compilation time is bounded to ≤ 2 s. Exceeding the cap surfaces as
     a `ScriptError` with `Code = "SCRIPT_COMPILATION_TIMED_OUT"`.
   - The compiler is invoked with the explicit reference list from
     `RoslynUserScriptCompilerHelpers` — never with `MetadataReferenceResolver`
     defaults that would let a script reference arbitrary assemblies.

2. **Surface leakage.** The globals must never expose a raw domain or
   framework type. `ScriptableProjector` is the only place that constructs
   the globals; flag any new property/method that returns a
   `HypertextTransferProtocolRequestData`, `HeaderCollection`, `TrafficFlow`,
   or any other mutable domain type. Always wrap in a `Scriptable*`
   projection.

3. **ALC lifecycle.** The `AssemblyLoadContext` is collectible. Validate:
   - No reference to a script-defined type is held in long-lived
     domain/framework state.
   - No script-defined type is registered into the DI container.
   - Subscriptions to `IDomainEventBus` from within the script ALC are
     either prevented at the projection boundary or use weak references.
   - On script removal or auto-disable, the previous ALC is unloaded
     **after** the new registration is swapped in.

4. **Cancellation contract.** Every invocation runs under a per-call
   `CancellationTokenSource` linked to the proxy listener's token. Validate:
   - Linking is unconditional — no path executes user code without a
     linked token.
   - The cancellation honours the configured wall-clock timeout (default
     5 s, range 1–60 s).
   - The cancellation propagates into `await` points inside the script.

5. **Memory ceiling.** The sandbox tracks allocations through
   `GC.GetAllocatedBytesForCurrentThread()` checkpoints. Validate:
   - The ceiling is configurable (default 50 MB, range 10–500 MB).
   - On breach, the invocation is cancelled, the ALC is unloaded, and
     the script is auto-disabled with `Code = "SCRIPT_MEMORY_EXCEEDED"`.

6. **Capability allow-list.** The set of namespaces and types reachable
   from a script is the allow-list in `RoslynUserScriptCompilerHelpers`.
   Flag any change that broadens the allow-list without an explicit
   `ScriptError` test that proves the new capability does not escape the
   sandbox — particularly `System.IO`, `System.Net`, `System.Threading`,
   `System.Reflection.Emit`, `System.Runtime.InteropServices`.

7. **Error-handling matrix.** Confirm each branch:
   | Failure | Effect |
   |---|---|
   | Compile error | Script invalid; traffic unmodified; diagnostics surfaced. |
   | Runtime exception | Caught; `Code = "SCRIPT_RUNTIME_FAILURE"`; traffic unmodified. |
   | Timeout | Invocation cancelled; traffic unmodified; script remains enabled. |
   | Memory overrun | ALC unloaded; script auto-disabled. |

   A change that lets an exception escape `UserScriptingHandler` is a
   correctness defect — the proxy pipeline must continue.

8. **Hot-reload.** On `MutableScriptingConfigurationChanged`:
   - The new script set compiles on a background task without blocking
     the pipeline.
   - The live registration swaps atomically only after compilation
     succeeds.
   - The previous ALC unloads last, after the swap.

9. **Logging.** Per-invocation logs are `Information` for start/complete,
   `Warning` for timeout/exception. The script's `request` / `response`
   content never appears in logs.

10. **Tests.** A new scriptable surface requires a `Domain.Scripting.Tests`
    case that compiles a script exercising it against a stub flow. A new
    sandbox capability requires a paired negative test confirming the
    capability is **not** reachable when it should not be.
