---
applyTo: "src/Domain.Scripting/**/*.cs"
---

# Roslyn scripting sandbox rules

`Domain.Scripting` compiles and runs user-supplied C# against the proxy
pipeline. A buggy sandbox lets a single script crash the proxy, drain memory,
or read files outside the sandboxed directory. Every change to this module is
reviewed against the constraints below.

## Compilation surface

- The compiler is `Microsoft.CodeAnalysis.CSharp.Scripting`. Scripts compile
  into a `RoslynUserScript` and expose `OnRequest` / `OnResponse` methods.
- Compilation errors surface as `ScriptDiagnostic` entries on
  `ScriptCompilationResult`. Treat **error**-severity diagnostics as
  "script invalid"; warnings do not block execution.
- A compilation budget of **2 seconds** governs each script — exceed it and
  the compile is treated as failed.

## Execution surface

- Scripts run inside their own `AssemblyLoadContext` so unload is possible.
  The ALC is created when the script first loads and unloaded when the script
  is removed or auto-disabled.
- The exposed globals are `RequestScriptGlobals` (request phase) and
  `ResponseScriptGlobals` (response phase). Both expose:
  - `ScriptableRequest request` / `ScriptableResponse response` — read/write
    surfaces wrapping the live `HypertextTransferProtocolRequestData` /
    `HypertextTransferProtocolResponseData`.
  - `ScriptableHeaders headers` — keyed, case-insensitive header access.
  - `IDictionary<string, object?> sharedState` — per-flow, per-script.
- The `ScriptableProjector` constructs the globals — keep additions to the
  surface explicit and additive. **Never** expose a mutable reference to a
  raw `HeaderCollection`, `TrafficFlow`, or any domain type; always wrap it
  in a `Scriptable*` projection.

## Forbidden capabilities

The sandboxed script must not be able to:

- Open files or sockets (no `System.IO.File`, `System.Net.Sockets`,
  `System.Net.Http`, `System.Net.HttpClient`).
- Spawn threads (no `Thread`, `Task.Run` against user code,
  `ThreadPool.QueueUserWorkItem`).
- Use reflection emit (no `System.Reflection.Emit`, no `DynamicMethod`).
- Load assemblies dynamically (`Assembly.LoadFrom`, `Assembly.LoadFile`,
  `AssemblyLoadContext.LoadFromAssemblyPath`).
- Invoke unmanaged code (`DllImport`, `LibraryImport`, function pointers,
  `Marshal`).
- Access process / environment internals (`Process`, `Environment.SetEnvironment*`,
  `Environment.Exit`).
- Sleep or block (`Thread.Sleep`, `Task.Delay`, infinite loops).

The compiler's allow-list is set in
`RoslynUserScriptCompilerHelpers` — if a new API needs to be reachable from a
script, it is added there, not by importing the namespace into the globals.

## Resource limits

Defaults and ranges enforced by the sandbox:

| Limit | Default | Range |
|---|---|---|
| Memory ceiling per script | 50 MB | 10–500 MB |
| Wall-clock timeout per invocation | 5 s | 1–60 s |

Timeout enforcement is via a per-invocation `CancellationTokenSource` linked to
the proxy's cancellation token. Memory enforcement watches the ALC's
allocations through `GC.GetAllocatedBytesForCurrentThread()` checkpoints.

When a script exceeds either limit:

- The invocation is cancelled; the script returns "no modification".
- The ALC is unloaded and the script is auto-disabled with a `ScriptError`
  describing the cause.

## Error handling

`UserScriptingHandler` is the integration point. The error matrix:

| Failure | Effect |
|---|---|
| Compile error | Script marked invalid; traffic unmodified; user sees diagnostics in the UI. |
| Runtime exception during invocation | Exception caught; `ScriptError` logged with `Code = "SCRIPT_RUNTIME_FAILURE"`; traffic unmodified. |
| Wall-clock timeout | Invocation cancelled; traffic unmodified; script remains enabled. |
| Memory overrun / OOM | ALC unloaded; script auto-disabled with `Code = "SCRIPT_MEMORY_EXCEEDED"`. |

Never re-throw an exception out of a script invocation — the proxy pipeline
must keep running. Errors propagate as `Result` values up through
`IScriptingHandler`.

## ALC lifecycle

The ALC is **collectible**. To ensure the runtime can actually unload it:

- Do not store references to script-defined types in long-lived domain or
  framework state.
- Do not register a script-defined type into the DI container.
- Subscribe scripts to events only through weak handlers; otherwise the bus
  pins the ALC forever.

## Configuration

Scripts and their per-host enablement live in `MutableScriptingConfiguration`.
Changes publish `MutableScriptingConfigurationChanged` on `IDomainEventBus`.
Hot-reload must:

1. Resolve the new script set.
2. Compile new scripts on a background task (without blocking the pipeline).
3. Atomically swap the live registration only after compilation succeeds.
4. Unload the previous ALC last, after the swap completes.

## Logging

Script invocation logs are at `Information` level for "started" / "completed"
and `Warning` for "timeout" / "exception". The script's own
`request`/`response` content is never logged.

## Adding a new scriptable surface

1. Add the new property/method to the appropriate `Scriptable*` wrapper —
   never expose the underlying domain type.
2. Update `ScriptableProjector` to populate it.
3. Add a regression test in `Domain.Scripting.Tests` that compiles and runs
   a script exercising the new surface against a stub flow.
4. Update the user-facing scripting reference docs.
