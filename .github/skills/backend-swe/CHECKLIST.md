# Backend SWE checklist

Detailed reference for the `backend-swe` skill, covering both whole-codebase
analysis and PR-diff review.

## Analysis

1. **API contracts.** Validate signatures are consistent and follow Proxyfan's
   conventions: returning `Result<T>` / `VoidResult` instead of throwing,
   accepting `CancellationToken` as the last parameter, using `ValueTask` for
   hot paths that frequently complete synchronously. Flag breaking changes to
   public or protected members on `Domain.*` types.

2. **Error handling.**
   - Public domain methods translate exceptions into a `DomainError`
     subclass and return `Result<>`. Never let a raw `Exception` escape.
   - Each bounded context uses its own `DomainError` subtype
     (`ProxyError`, `RuleError`, `ScriptError`, `CertificateError`,
     `SessionError`, …) with a `Code` constant (`"PROXY_BIND_FAILED"`,
     `"SCRIPT_RUNTIME_FAILURE"`, …).
   - Generic `catch (Exception)` blocks must either narrow or log with
     enough context to debug.
   - The proxy pipeline must keep running on a per-connection failure —
     no `throw`s that propagate to the dispatcher.

3. **Logging and observability.**
   - Use `ILogger<T>` (or `ILoggerFactory.CreateLogger(…)`) and structured
     fields (`{ConnectionId}`, `{FlowId}`, `{Host}`, `{StatusCode}`).
   - Never log request or response **bodies**.
   - Never log raw `Authorization`, `Cookie`, `Set-Cookie` values — they
     are redacted at every level.
   - `Trace` is the only level at which headers are logged.
   - Flag missing logs on failure paths and at key decision points;
     flag over-logging on hot proxy paths.

4. **Configuration / options binding.**
   - Options are bound via `Microsoft.Extensions.Options`; the
     `Automaticks.Extensions.Options.Analyzers` package enforces several
     rules — fix the diagnostic, do not silence it.
   - Strongly-typed options live alongside their service in the matching
     `Domain.*` project (e.g. `ProxyOptions`, `ReverseProxyOptions`,
     `UpstreamProxyOptions`).
   - Validation goes through `IValidateOptions<T>`
     (`ProxyOptionsValidator`) and fails fast on startup; never validate
     options lazily inside a hot method.
   - Hot-reload only for settings explicitly marked as reloadable — the
     proxy port, upstream chain, and listener config require restart.

5. **Security basics.**
   - Inputs from the network and from disk are untrusted. Validate length,
     shape, and charset before parsing.
   - DPAPI is the encryption primitive for at-rest secrets (root CA
     private key in `%LOCALAPPDATA%\Proxyfan\certificates\`).
   - No hardcoded credentials, no API keys, no PII in source.

6. **Data access patterns.**
   - In-memory stores (`TrafficStore`, `WebSocketStore`,
     `ServerSentEventsStore`, `RemoteProcedureCallStore`) are append-mostly
     with bounded capacity and oldest-first eviction.
   - File-system writes (HAR, configuration, logs, certificates) go through
     atomic write-and-rename — never write in place to a path the user
     might read.

7. **Anti-patterns.**
   - **God classes.** Long files in `Framework.Networking` (e.g. the
     `HypertextTransferProtocolForwarder*` family) are intentionally split
     into many small types with `*Dependencies` records — follow that
     pattern.
   - **Hidden coupling.** A service that injects another service of the
     same bounded context for "convenience" instead of going through the
     event bus.
   - **Async misuse.** See `asynchrony` skill.

8. **Async correctness.**
   - `CancellationToken` is the last parameter on every async method
     (`ATXTA008`).
   - `CancellationToken` parameters never have default values (`ATXCS005`).
   - Tasks are never left unobserved (`ATXTA010`).
   - No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` on `Task`.
   - No `async void` outside Avalonia event handlers.

9. **Naming.**
   - `Async` suffix on `Task`-returning methods (`ATXCS003`).
   - `Async` suffix only on `Task` / `ValueTask` returning methods
     (`ATXCS009`).
   - Boolean methods start with `Can` or `Has`; boolean properties with
     `Is` or `Allow`.

10. **Delegate types.** Prefer named delegates over `Action`, `Func`,
    `Predicate`, `Comparison`, `Converter` (`ATXCS020`). The
    `Framework.Networking` callbacks (`RemoteProcedureCallMessageCallback`,
    `ServerSentEventCallback`, `WebSocketMessageCallback`) demonstrate the
    pattern.

11. **Dependencies records.** When a constructor is approaching four
    parameters, refactor into a `*Dependencies` record (see
    `HypertextTransferProtocolProxyHandlerDependencies`,
    `TransportLayerSecurityInterceptorHandlerDependencies`,
    `HypertextTransferProtocolVersion2OrchestratorDependencies`). The DI
    factory builds the record once.

12. **Inline `new` in arguments** (`ATXCS058`) — assign to a named local first.

13. **Blank-line discipline** — at most one blank line between two constructs.

14. **Default parameter values** are forbidden (`ATXCS057`).
