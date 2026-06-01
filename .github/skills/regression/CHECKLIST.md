# Regression checklist

Detailed reference for the `regression` skill, covering both whole-codebase
analysis and PR-diff review.

## Analysis

1. **Change inventory.** Enumerate the changed files, classes, and methods.
   For a PR diff use `gh pr diff <N>`; for a branch use
   `git diff <base>..HEAD`; for a whole-codebase analysis, focus on recent
   commits (`git log --since`) unless a specific scope is named.

2. **Impact analysis.** For each changed component, identify callers,
   dependents, and downstream consumers. Flag components with wide blast
   radius:
   - `ProxyServer` → all listeners, all handlers.
   - `IRuleEngine` → every handler that consults rules before forwarding.
   - `IDomainEventBus` and its subscribers — a change to a published event
     ripples to every subscriber.
   - Public types in `Domain.<X>` — every Presentation or CLI consumer.

3. **Baseline comparison.** For modified methods, compare the new behaviour
   against the prior implementation:
   - Return type / value changes.
   - Exception conditions added or removed.
   - Side-effect changes (events raised, store mutations, files written).
   - Pre/post-conditions.
   - Serialisation format changes (HAR shape, YAML config shape).

4. **Missing regression tests.** For each modified method or class, verify
   a test exercises the changed behaviour. If a test was removed, check
   whether the coverage was intentionally dropped.

5. **High-risk surfaces.** Treat changes here as critical regression
   candidates:
   - **Proxy core** — `ProxyServer`, `SocketProxyListener`,
     `ConnectionDispatcher`, all `IConnectionHandler` implementations.
   - **Rule engine** — `RuleEngine`, `RuleRegistry`, and the discriminated
     unions `RequestPipelineAction` / `ResponsePipelineAction` (adding or
     reshuffling a case ripples through every consumer of the union).
   - **Traffic store** — `TrafficStore`, `WebSocketStore`,
     `ServerSentEventsStore`, `RemoteProcedureCallStore` (eviction,
     observation, capacity).
   - **HTTP/2 surface** — `HypertextTransferProtocolVersion2*` family
     (HPACK, frame parsing, stream state machine, orchestrator).
   - **TLS interception** — `TransportLayerSecurityInterceptorHandler`,
     `CertificateAuthority`, `LeafCertificateCache`, ALPN negotiation in
     `TransportLayerSecurityInterceptorHelpers`.
   - **Serialisation** — HAR import/export, content decoders, YAML
     read/write.
   - **Public contracts** on `Domain.*` types or on the extensibility
     interfaces (`IContentDecoder`, `ITrafficInspector`, `IExportFormatter`,
     `IConnectionHandler`, `IUserScript`).

6. **Behavioural drift.** Identify changes that alter observable behaviour
   without corresponding test updates. Particularly flag cases where
   existing tests may no longer test what they claim (stale test names,
   outdated assertions).

7. **Event-cascade ripple.** When a `IDomainEvent` payload or shape
   changes, walk every `DomainEventHandler<T>` subscription. The bus's
   `Publish<T>` is synchronous and exception-isolated; a subscriber that
   throws does not block siblings, but a contract change can still
   produce silent misbehaviour. Validate that every subscriber compiles
   and that the integration cascade is still covered.

8. **Validation steps.** For each high-risk area, define concrete
   validation steps:
   - Tests to run (single-test or project-scoped via `Run-Tests.ps1`).
   - Scenarios to exercise manually (smoke tests for UI / E2E surfaces).
   - Sibling features that could break — list them so the human can
     spot-check.

## Risk matrix

| Risk | Criteria |
|---|---|
| High | Public contract, proxy hot path, rule engine, traffic store, TLS interception, HAR shape, YAML config shape, certificate trust, scripting sandbox |
| Medium | Cross-context event payloads, ViewModel projections used by multiple Views, throttle profile shape, reverse-proxy route registry |
| Low | Isolated changes inside one slice with narrow dependents |

## Forbidden silencers in proposed fixes

Tests cannot be `[Skip]`-ed, deleted, or weakened to clear a regression.
Coverage gates cannot be lowered. If a behaviour change is intentional, the
matching test is updated with a cited justification linking to the new
expected behaviour.
