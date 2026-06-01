# Proxy-pipeline checklist

Detailed reference for the `proxy-pipeline` skill.

## Surfaces

- `Domain.Proxy/ProxyServer.cs` — lifecycle (`Start`, `Stop`, status).
- `Domain.Proxy/IProxyListener.cs` and
  `Framework.Networking/SocketProxyListener.cs` — TCP accept loop.
- `Domain.Proxy/IConnectionDispatcher.cs` and
  `Framework.Networking/ConnectionDispatcher.cs` — protocol detection and
  handler selection.
- `Domain.Proxy/IConnectionHandler.cs` and its implementations:
  - `HypertextTransferProtocolProxyHandler` (HTTP/1.1 forward proxy).
  - `ConnectTunnelHandler` (CONNECT tunnelling).
  - `TransportLayerSecurityInterceptorHandler` (intercepted HTTPS).
  - `SocksTunnelHandler` (SOCKS 4/5).
  - `HypertextTransferProtocolVersion2Orchestrator` (HTTP/2 inside an
    intercepted TLS session).
  - `WebSocketUpgradeTunnel` (WebSocket after HTTP/1.1 upgrade).
  - `ServerSentEventsStreamHandler` (SSE response relay).
- `HypertextTransferProtocolForwarder` and its
  `HypertextTransferProtocolForwarderDependencies` — the outbound
  HTTP/1.1 request execution.
- `Domain.Proxy/ReverseProxyOptions.cs`,
  `ReverseProxyRouteRegistry`, `ReverseProxyRouteListener`,
  `ReverseProxyEngine`, `ReverseProxyHypertextTransferProtocolHandler` —
  reverse-proxy mode.
- `Domain.Proxy/PeriodicReverseProxyHealthChecker.cs` and
  `Framework.Networking/TransportControlProtocolBackendHealthProbe.cs` —
  health checks.
- `Framework.Networking/AcceptErrorClassifier.cs` —
  the only classifier for accept errors; gates which exceptions are
  fatal versus transient.

## Analysis

1. **Listener correctness.** `SocketProxyListener` accepts on a bound TCP
   port. Validate:
   - Bind failures translate to `ProxyBindError` and surface as a
     `Result.Failure`.
   - Already-running prevention surfaces `ProxyAlreadyRunningError`.
   - Accept exceptions go through `AcceptErrorClassifier` — never let a
     transient socket error tear down the listener.
   - Each accepted connection runs in an independent task that owns its
     own cancellation token linked to the listener's token.

2. **Dispatcher decision tree.** `ConnectionDispatcher` peeks the first
   bytes to choose:
   - HTTP/1.1 method prefix → forward / CONNECT handler.
   - HTTP/2 connection preface → orchestrator.
   - SOCKS 4/5 greeting bytes → SOCKS tunnel handler.
   - TLS ClientHello (when intercepted on the public listener) → TLS
     interceptor.
   - Add a new protocol by registering a sibling handler, not by
     extending an existing handler's `switch`.

3. **Handler contracts.** Every `IConnectionHandler`:
   - Owns its connection until completion; releases the socket and
     pipes in a `finally` block.
   - Propagates the listener's `CancellationToken` to every downstream
     async call.
   - Translates exceptions into typed errors before logging — never
     swallow silently.
   - Records the outcome through a `HypertextTransferProtocolFlowEventPublisher`
     (or the equivalent for non-HTTP protocols).

4. **Forwarder outcomes.** `HypertextTransferProtocolForwardingOutcome`
   is the discriminated union of forwarding results. Each case has a
   distinct down-stream behaviour (rule action applied, breakpoint
   pause, scripted modification, local response, raw forward, upstream
   error). Adding a new case ripples through every consumer — confirm
   the change is intentional and update the matching tests.

5. **CONNECT tunnelling.** `ConnectTunnelHandler` opens the upstream
   socket, sends `200 Connection Established`, and bridges
   bidirectionally via `BidirectionalStreamPump`. Validate:
   - The upstream connect honours the `UpstreamProxyOptions` chain
     (`UpstreamForwardingTarget`).
   - `BidirectionalStreamPump` aborts both directions on either side's
     close, without leaking buffers.
   - `ConnectTargetValidator` runs before any upstream connect.

6. **Reverse-proxy mode.** `ReverseProxyEngine` registers routes through
   `ReverseProxyRouteRegistry`; per-route listeners
   (`ReverseProxyRouteListener`) bind their ports. Validate:
   - Route state changes publish `ReverseProxyRouteStatusChanged` on
     `IDomainEventBus`; do not mutate state directly from a handler.
   - Health checks unbind a route until recovery.
   - `ReverseProxyHostHeaderRewriter` correctly preserves or rewrites
     the `Host` header per the route's mode.

7. **Listener teardown.** `Stop` cancels the listener token, awaits
   pending accepts, and unbinds the socket. Validate no orphan tasks
   linger past `Stop` and no second `Start` can race a stopping
   listener.

8. **System-proxy registration.** `ISystemProxy` (in `Domain.Proxy`)
   is the abstraction; `Framework.Platform` implements the Windows
   registration. The handler must:
   - Restore the previous system proxy on `Stop` (when registration
     was performed by Proxyfan).
   - Refuse to register when another proxy is already configured
     unless the user opts in explicitly.

9. **Performance budget cross-cuts.**
   - Per-request overhead < 1 ms (excluding rules / throttling).
   - 10,000+ concurrent connections.
   - Allocation discipline — see `performance/CHECKLIST.md`.

10. **Logging.** Every handler logs accept / dispatch / completion at
    `Information`, transient errors at `Warning`, unexpected exceptions
    at `Error`. Headers only at `Trace`. Bodies never. Structured fields
    are mandatory.
