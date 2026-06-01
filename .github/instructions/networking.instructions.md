---
applyTo: "src/Framework.Networking/**/*.cs,src/Domain.Proxy/**/*.cs"
---

# Networking and proxy-pipeline rules

The proxy engine is the highest-throughput, lowest-latency surface in the
codebase. Bugs here corrupt user traffic, leak memory at line rate, or stall
every captured connection. Every change in `Framework.Networking` or
`Domain.Proxy` is reviewed against this file.

## Performance budget

The end-to-end overhead added by the proxy must remain under **1 ms per
request** (excluding rule evaluation and throttling). To stay inside that
budget:

- Build on `System.IO.Pipelines` for byte-stream I/O. Do not allocate a
  per-frame `byte[]`; use `ArrayPool<byte>.Shared` or the pipe's own buffer.
- Use `Span<T>` / `ReadOnlySpan<T>` for parsing. Avoid materialising a
  `string` from a header line until the rule pipeline actually inspects it.
- Use `ValueTask` for hot async paths that frequently complete synchronously
  (header parsing, frame reads, throttle waits).
- Pool large buffers (`MemoryPool<byte>`) for body relay; never allocate the
  full response body up front.

## CancellationToken propagation

- Every async method in `Framework.Networking` accepts a `CancellationToken`
  as the **last** parameter (`ATXTA008`). The token from the proxy listener
  flows all the way down to socket reads, TLS handshakes, and HPACK parsing.
- Never create a fresh `CancellationToken` inside a handler — link to the
  caller's token with `CancellationTokenSource.CreateLinkedTokenSource` if a
  local timeout is needed.
- Tokens never carry default values (`ATXCS005`).

## Sync-over-async is banned

`.Result`, `.Wait()`, `.GetAwaiter().GetResult()` on a `Task` are forbidden in
the proxy pipeline. They will deadlock under the synchronisation context of
the connection dispatcher and stall every captured connection on that listener.

## `async void` is banned

The only legitimate `async void` is an Avalonia event handler in
`Presentation` code-behind. Networking code is never UI code; use
`async Task` and propagate exceptions through the result type.

## Connection dispatch

`IConnectionDispatcher` peeks the first bytes of a new TCP connection to
decide between HTTP/1.1, HTTP/2 prior knowledge, CONNECT tunnelling,
SOCKS 4/5, and TLS interception. The protocol detector lives in
`SocksProtocolDetector` / `HypertextTransferProtocolMethodPrefixDetector`.
Add a new protocol by registering a sibling `IConnectionHandler`, not by
extending the dispatcher's `switch`.

## TLS interception

The MITM path lives in `TransportLayerSecurityInterceptorHandler` and friends.
Every change here must preserve:

- The ALPN advertised to the client mirrors the protocol selected upstream
  (HTTP/1.1 ↔ HTTP/1.1, h2 ↔ h2). `TransportLayerSecurityInterceptorHelpers`
  centralises the negotiation.
- Leaf certificates are signed by the root CA in `Domain.Certificates` and
  cached in `LeafCertificateCache` (LRU, default 1000 entries). New
  certificates are minted only on cache miss.
- The SNI proxying allow-list (`ServerNameIndicationProxyingList`) gates which
  hosts get interception. Hosts outside the list pass through as raw TCP
  tunnels.
- Decrypted bytes are bridged through `TransportLayerSecurityInterceptionPipes`,
  not through `SslStream`-level copies.

## HTTP/2 specifics

`HypertextTransferProtocolVersion2Orchestrator` operates on raw frames and
shadow-decodes `HEADERS` / `CONTINUATION` / `DATA` for capture, **without**
re-encoding HPACK on the wire. Rules and scripting do not apply to HTTP/2
traffic — the pipeline is capture + inspect only. Do not introduce
mutation hooks for HTTP/2 without first updating the architecture decision
and the protocol-parsers skill.

The HPACK decoder (`HypertextTransferProtocolVersion2HpackDecoder`) and
encoder (`HypertextTransferProtocolVersion2HpackEncoder`) are symmetric;
when extending the static table, update both sides together.

## WebSocket, SSE, gRPC

Long-lived protocols (`WebSocketRelay`, `ServerSentEventsRelay`,
`RemoteProcedureCallRelay`) keep a per-connection buffer of recent messages.
Budgets:

- WebSocket / gRPC: 1,000 messages per connection (configurable).
- SSE: 5,000 events per connection (configurable).
- Global cap across streaming protocols: 200 MB (configurable).

When the budget fills, evict the oldest entries first — never block the
upstream pump, never grow without bound.

## Upstream proxy chaining

`UpstreamProxyOptions` describes the upstream chain. Tests cover the
HTTP/HTTPS/SOCKS/PAC permutations plus bypass-list matching and basic-auth
header injection (`ProxyAuthorizationHeader`). When adding a new upstream
variant, mirror the existing test coverage exactly — chaining bugs leak
credentials, and credentials leaks are a P1 security finding.

## Throttling

`Domain.Throttling` ships `TokenBucket` and a stable preset list
(`ThrottleProfilePresets`). Apply throttling via `ThrottleApplier` and
`ThrottledStreamWriter`. Never throttle on the dispatcher thread — the bucket
is per-connection scope.

## Reverse proxy

`ReverseProxyEngine` and `ReverseProxyRouteListener` host the reverse-proxy
mode. Health checks come from `IBackendHealthProbe` /
`TransportControlProtocolBackendHealthProbe` on a periodic schedule
(`PeriodicReverseProxyHealthChecker`). Route state changes publish through
`ReverseProxyRouteStatusChanged` on `IDomainEventBus`; never mutate route
state directly from a handler — go through the registry.

## Error handling

Bind socket / TLS / HPACK exceptions to a typed `ProxyError` subclass and
return a `Result<>` to the caller. The pipeline must keep running when a
single connection fails — log the error at `Warning` (or `Error` for
unexpected exceptions), drop the connection, and accept the next one.

## Logging and privacy

- Bodies are **never** logged. Headers are logged only at `Trace` level.
- `Authorization`, `Cookie`, and `Set-Cookie` are redacted by default at all
  levels. The redactor in the logging pipeline owns the policy — do not
  re-implement the filter elsewhere.
- Diagnostic logging uses structured fields (`{ConnectionId}`, `{Host}`,
  `{Method}`, `{StatusCode}`, `{Duration}`) so log queries stay tractable.

## Concurrency

- `HypertextTransferProtocolVersion2StreamRegistry` and similar registries are
  single-writer / multi-reader. The writer is the orchestrator pump; readers
  are the rule pipeline. Never write from a reader thread.
- `ConcurrentDictionary.GetOrAdd(key, valueFactory)` may invoke the factory
  more than once on contention. Use `TryGetValue` + `TryAdd` for factories
  with side effects (registration, file creation).
- `Interlocked` is preferred over `lock` for counter updates.

## Adding a new protocol handler

1. Implement `IConnectionHandler` in a new file under `Framework.Networking`.
2. Register it in `DependencyInjection/ServiceCollectionExtensions.cs`.
3. Update `ConnectionDispatcher` to recognise the protocol's wire signature.
4. Add a `*Tests.csproj` mirror with parser, framing, and end-to-end tests.
5. Update `docs/ARCHITECTURE.md` § 12.3 and the `protocol-parsers` skill
   catalogue.
