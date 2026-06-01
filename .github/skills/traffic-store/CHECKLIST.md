# Traffic-store checklist

Detailed reference for the `traffic-store` skill.

## Surfaces

- `Domain.Traffic/TrafficStore.cs` — primary HTTP request/response flow
  store; backed by `ITrafficStore`.
- `Domain.Traffic/WebSocketStore.cs`, `WebSocketFlow.cs`,
  `WebSocketFlowClosedHandler.cs`, `WebSocketMessageRecordedHandler.cs`,
  `IWebSocketStore.cs` — WebSocket flows and per-message recording.
- `Domain.Traffic/ServerSentEventsStore.cs`, `ServerSentEventsFlow.cs`,
  `ServerSentEventsFlowClosedHandler.cs`,
  `ServerSentEventsFlowEventRecordedHandler.cs`, `IServerSentEventsStore.cs`.
- `Domain.Traffic/RemoteProcedureCallStore.cs`,
  `RemoteProcedureCallFlow.cs`, `RemoteProcedureCallFlowClosedHandler.cs`,
  `RemoteProcedureCallFlowMessageRecordedHandler.cs`,
  `IRemoteProcedureCallStore.cs`.
- `Domain.Traffic/TrafficFlow.cs`, `TrafficFlowOrigin.cs`,
  `TrafficFlowStatus.cs`, `TrafficFlowColorTag.cs`.
- `Domain.Traffic/TrafficFilter.cs` — UI-facing filter that subsets the
  store.
- `Domain.Traffic/Diff/`, `Columns/`, `Tabs/` — UI projection helpers.
- `Domain.Traffic/ComposerHistoryService.cs`, `ComposerHistoryEntry.cs`,
  `IComposerHistoryStore.cs` — replay/composer history.

## Defaults

| Setting | Default | Range |
|---|---|---|
| `capture.maxFlows` | 10,000 | 100 – 1,000,000 |
| `capture.largeBodyThreshold` | 1 MB | 64 KB – 100 MB |
| WebSocket / gRPC per-connection message buffer | 1,000 | configurable |
| SSE per-connection event buffer | 5,000 | configurable |
| Streaming-protocol global memory budget | 200 MB | configurable |

## Analysis

1. **Eviction.** All four stores enforce a capacity. When full, oldest
   entries are evicted first. Validate:
   - Eviction is O(1) amortised, not O(n).
   - Eviction releases large-body spill files promptly.
   - Eviction never leaves a UI subscriber holding a reference to a
     destroyed flow.
   - Capacity changes apply atomically; partial reconfiguration is not
     allowed.

2. **Observation pipeline.** Each store exposes observation hooks
   (handlers / events) consumed by the UI to update bindings. Validate:
   - Hooks fire after the flow is in a consistent state, not mid-update.
   - Hooks are dispatched in append order — the UI relies on append-only
     semantics.
   - Hook exceptions never break the storing pump.

3. **Large-body spill.** Responses larger than
   `capture.largeBodyThreshold` spill to a temp file in
   `%LOCALAPPDATA%\Proxyfan\…`. Validate:
   - The spill file is deleted when the flow is evicted.
   - Concurrent reads from the UI use shared-read access.
   - The path is generated with a non-guessable suffix to avoid trivial
     collisions.

4. **Memory pressure strategy.**
   - Soft pressure (> 70 %): warning logged.
   - Hard pressure (> 90 %): evict 10 % of oldest flows.
   - Critical pressure (OS-signalled): pause capture and surface to UI.

   Validate the thresholds and the actions are honoured. Cross-reference
   with `performance/SKILL.md`'s memory budgets.

5. **Filter performance.** `TrafficFilter` runs on every UI list update.
   It must be fast enough to scan 100,000 flows on a virtualization
   refresh. Flag any filter that allocates per-row.

6. **Concurrency.** The stores are single-writer (the capture pump)
   multi-reader (the UI). Validate:
   - Writers do not hold a `lock` across an `await`.
   - Readers see a consistent snapshot — either a true immutable
     snapshot or a stable concurrent collection iterator.
   - Eviction synchronises with readers so a reader cannot observe a
     half-destroyed flow.

7. **WebSocket / SSE / gRPC budgets.** Per-connection buffers cap the
   number of messages retained. A new message beyond the cap evicts the
   oldest. Validate the global streaming-budget circuit breaker cuts in
   before unbounded growth.

8. **Composer history.** `ComposerHistoryService` retains recent composed
   requests. Validate retention bounds, persistence across sessions
   (if applicable), and absence of body retention beyond the configured
   policy.

9. **Diff and column projections.** `Diff/` produces request/response
   diffs for the UI. `Columns/` projects flows into bound UI columns
   (status, method, URL, content type, duration). Flag projections that
   recompute on every layout pass instead of caching the projection on
   the flow.

10. **Privacy on store.** Bodies never leave the store in their raw form
    for any logging or telemetry path. The redactor in the logging
    pipeline owns header redaction; store-level projections used for
    diagnostics must consult the same redaction policy.
