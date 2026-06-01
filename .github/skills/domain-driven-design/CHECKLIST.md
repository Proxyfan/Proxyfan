# Domain-driven design checklist

Detailed reference for the `domain-driven-design` skill, covering both
whole-codebase analysis and PR-diff review. Read `PERSONA.md` first — the
four-question hard rule gates every item on this list.

## Bounded contexts in Proxyfan

| Context | Project(s) | Aggregate-root candidates |
|---|---|---|
| Proxy lifecycle | `Domain.Proxy` | `ProxyServer`, `ReverseProxyRouteRegistry`, `ReverseProxyRoute` |
| Traffic capture | `Domain.Traffic` | `TrafficStore`, `TrafficFlow`, `WebSocketFlow`, `ServerSentEventsFlow`, `RemoteProcedureCallFlow`, `ComposerHistoryService` |
| Rule pipeline | `Domain.Rules` | `RuleEngine`, `RuleRegistry`, each `Mutable*Rule` |
| Scripting | `Domain.Scripting` | `MutableScriptingConfiguration`, `RoslynUserScript` |
| Certificates | `Domain.Certificates` | `CertificateAuthority`, `LeafCertificateCache`, `ServerNameIndicationProxyingList`, `MutableCertificateAuthorityProvider` |
| Session | `Domain.Session` | (HAR document — exporter / importer types) |
| Throttling | `Domain.Throttling` | `MutableThrottleProfile`, `TokenBucket` |
| Configuration | `Domain.Configuration` | `ConfigurationSnapshot`, `UserPreferences` |
| DNS spoofing | `Domain.DomainNameSystemSpoofing` | (spoofing entry / registry types) |
| Updates | `Domain.Updates` | (update-check types) |
| Remote devices | `Domain.RemoteDevices` | (remote-device types) |

## Analysis

1. **Bounded-context integrity.** For each context, confirm it owns a
   coherent slice of the business and exposes only translated contracts at
   its boundary. Flag direct consumption of another context's internal type,
   shared-kernel additions in `Domain` that actually belong to one context,
   and `Framework.*` types reaching back into `Domain.*` signatures.

2. **Aggregate boundaries.** For each aggregate root candidate, verify:
   - Every invariant lives inside the aggregate, not in handlers or
     ViewModels that touch it.
   - Transactional consistency is correct — one command does not silently
     mutate two aggregates that should converge via events.
   - The aggregate does not expose internal collections for outside
     mutation; return read-only views and route mutations through methods.

3. **Entities vs value objects.** Flag entities with no identity (should be
   value objects: `BasicAuthenticationCredentials`, `ContentType`,
   `QueryParameter`, `WebSocketOpcode`, `TimingPhase` …) and value objects
   that have grown mutable state or identity (should be promoted to entities).

4. **Misnamed services / managers / helpers.** Domain services exist for
   behaviour that does not belong to a single aggregate. Flag types whose
   only job is to orchestrate one aggregate's methods — push the behaviour
   into the aggregate. Flag vague suffixes; treat them as a missing-concept
   finding, not a style nit (`code-health` covers the style mechanics).

5. **Domain events.** Cross-context side effects route through
   `IDomainEventBus`. Flag:
   - Handlers that synchronously call into a second aggregate.
   - Events named after a technical change (`SomethingUpdated`,
     `ModelChanged`) instead of a business fact (`TrafficFlowCompleted`,
     `BreakpointPauseInboxChanged`, `ReverseProxyRouteStatusChanged`).
   - Events handled in only one place when multiple contexts should react.
   - Business rules implemented inside an event handler that actually
     belong inside the aggregate that raised the event.

6. **Ubiquitous language.** Sweep the diff or scope. Flag:
   - The same concept named differently across files (`Flow` vs `Request`
     vs `Exchange` for the same conceptual unit).
   - Technical jargon leaking into domain names (`Dto`, `Record`, `Row`,
     `Entity` as a noun on a domain concept).
   - Method names that describe state mutation (`SetX`, `UpdateY`) where a
     business verb exists (`Capture`, `Replay`, `Resume`, `Spoof`,
     `Intercept`, `Redirect`, `MapLocally`, `Trust`).
   - Boolean parameters that encode two distinct operations (split into two
     named methods).

7. **Business-rule ownership.** Every rule lives in the aggregate or domain
   service that owns the concept. Flag:
   - Rules enforced in command handlers, ViewModels, hubs, or repositories
     that should live in the aggregate.
   - The same rule expressed in both a server-side handler and a
     client-side ViewModel — share a domain query or domain method.
   - Persistence code computing derived state that should be derived inside
     the aggregate.

8. **API framing.** APIs express business intent, not data updates. Flag:
   - Commands shaped like CRUD (`UpdateXRecord(id, fields…)`) where a
     business operation exists (`PinCertificate`, `EnableMapLocal`,
     `AddBypassPattern`).
   - Multiple commands required for one business action — collapse them.
   - Query results that expose internal aggregate structure instead of the
     projection the caller needs.

9. **Infrastructure contamination.** Domain models must not depend on
   infrastructure types. Flag:
   - Serialisation attributes on a domain aggregate (move to a DTO in the
     `Framework.Serialization` adapter).
   - `ILogger<T>` inside an aggregate (logging is not a domain
     responsibility — raise a domain event and log in the subscriber).
   - Avalonia / `Presentation` types referenced from `Domain.*`.
   - `System.IO.File` / `System.Net.*` referenced from `Domain.*` (the
     domain talks to abstractions; concrete I/O lives in `Framework.*`).

## Proposal guidance

- Lead with the business concept, then the code change.
- Prefer evolving an existing aggregate over introducing a parallel one.
- Honour the event pipeline — never recommend a direct call between two
  aggregate roots.
- Match the subdomain: the proxy / rule / scripting surfaces are
  differentiating (rich models welcome); configuration / DNS spoofing /
  remote-device surfaces are supporting (stay thin).
- Apply tactical DDD patterns only when the finding documents the concrete
  pain the pattern relieves.
