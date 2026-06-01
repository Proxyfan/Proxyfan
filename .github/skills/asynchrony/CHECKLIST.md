# Asynchrony checklist

Detailed reference for the `asynchrony` skill. Apply every relevant item to
any change touching async / concurrent code.

## Concurrent surfaces in Proxyfan

- `ProxyServer` → `SocketProxyListener` → per-connection accept loop in
  `Framework.Networking`.
- `HypertextTransferProtocolVersion2Orchestrator` runs one pump per
  connection and shadow-decodes frames into the traffic store.
- `BidirectionalStreamPump` bridges client ↔ upstream for TLS-intercepted
  streams.
- `WebSocketRelay`, `ServerSentEventsRelay`, `RemoteProcedureCallRelay` run
  per-connection message loops with bounded message buffers.
- `TrafficStore`, `WebSocketStore`, `ServerSentEventsStore`,
  `RemoteProcedureCallStore` are mutated by per-connection pumps and read
  by the UI thread.
- `LeafCertificateCache` is an LRU read by many concurrent TLS handshakes.
- `PeriodicReverseProxyHealthChecker` runs a background loop checking
  upstream health.
- `Domain.Throttling/TokenBucket` is per-connection but referenced from
  multiple sibling pumps.

## Enforced rules

- `ATXTA008` — `CancellationToken` must be the last parameter on async
  methods.
- `ATXCS005` — `CancellationToken` parameters must not have default values.
- `ATXTA010` — tasks must not be left unobserved.
- `ATXCS003` — task-returning methods use the `Async` suffix.
- `ATXCS009` — methods with the `Async` suffix return `Task` or `ValueTask`.

## Analysis

1. **`CancellationToken` propagation.** Every async method accepts and
   forwards a `CancellationToken`. The proxy listener's token is the root —
   it flows down through accept, parse, forward, throttle, capture, and
   script invocation. Flag any method that creates a fresh token internally
   without linking to the caller's via
   `CancellationTokenSource.CreateLinkedTokenSource`.

2. **Pump lifecycle.** A pump must be cancelled before the underlying
   connection is torn down. Flag any close path that does not cancel the
   pump's token, dispose the pump's `*Dependencies` instance, or drain the
   pending writes.

3. **Stale captures after `await`.** Locals captured before an `await` are
   stale on resumption. The pattern that breaks Proxyfan is reading a
   `FocusedFlow` / `CurrentResponse` before an `await` and assuming it has
   not changed afterwards. Flag captures of mutable references that are
   read again after a yield without re-reading.

4. **`ConcurrentDictionary` misuse.** `GetOrAdd(key, factory)` may call the
   factory more than once on contention. Flag any factory with side
   effects (registration, file creation, allocation tracking). Use
   `TryGetValue` followed by `TryAdd` with the produced value when the
   factory has side effects.

5. **Volatile counters.** Counters touched by concurrent pumps need
   `Interlocked.Increment` / `Interlocked.Decrement`. Flag plain `++` /
   `--` on a shared field, even when the field is declared `volatile`.

6. **`async void`.** Forbidden outside Avalonia event handlers. `async void`
   swallows exceptions and prevents callers from observing completion.

7. **Sync-over-async.** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
   on `Task`-returning methods. Forbidden on the proxy hot path — they
   stall every captured connection on that listener.

8. **`ConfigureAwait(false)`.** Library code in `Domain.*` and `Framework.*`
   should use `ConfigureAwait(false)` on awaited tasks unless it genuinely
   needs the synchronisation context. The UI thread is the only place
   where the context matters.

9. **Unobserved tasks.** Flag fire-and-forget patterns: `_ = SomeAsync()`,
   `Task.Run(() => Method())` without an `await`, unawaited calls to async
   methods. All tasks must be observed or explicitly tracked.

10. **Dispatcher marshalling.** ViewModels and code-behind that resume from
    a non-UI `await` must marshal back through `Dispatcher.UIThread.Post`
    or `InvokeAsync` before touching a bound property. See
    `mvvm.instructions.md` and `avalonia.instructions.md`.

11. **Token-bucket atomicity.** `Domain.Throttling/TokenBucket` is the
    only place where throttle accounting happens. Flag duplicate
    bookkeeping in a sibling handler or a missed return after a throttled
    wait. The bucket must not hold a `lock` across an `await`.

12. **Script invocation cancellation.** `UserScriptingHandler` honours a
    per-invocation timeout via a linked token. Flag any path that runs
    user code without a linked cancellation, or that leaves the script's
    ALC pinned after a timeout.

13. **Disposable async.** `IAsyncDisposable` is preferred for resources
    that need an async cleanup. `DisposeAsync` is awaited in `finally`
    blocks or `await using` scopes.
