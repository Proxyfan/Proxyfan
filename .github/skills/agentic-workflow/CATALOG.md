# Specialist catalogue

Reference table for the `agentic-workflow` orchestrator. One sub-task is
dispatched per row. Each sub-task receives the scope (analysis mode) or the
diff (review mode) and is instructed via its skill name.

| Skill | Focus |
|---|---|
| `/architect` | Layer and module boundaries, dependency direction, circular references, ArchUnitNET conformance, architectural drift across `Domain.*` / `Framework.*` / `Presentation.*`. |
| `/domain-driven-design` | Bounded contexts (Proxy, Traffic, Rules, Scripting, Certificates, Session, Throttling, Configuration, DomainNameSystemSpoofing, Updates, RemoteDevices), aggregate boundaries, ubiquitous language, anemic models, infrastructure leaking into the domain. |
| `/backend-swe` | API contracts, `Result<T>` and `DomainError` usage, options binding (`Microsoft.Extensions.Options`), logging, error handling, anti-patterns, processor / handler shape. |
| `/bug-bounty` | Injection, auth/authz, deserialization, sensitive-data exposure, weak crypto, misconfiguration, TLS interception authority, certificate trust handling. |
| `/security-hardening` | Defence in depth — sandboxing, DPAPI usage, redaction policy, certificate cache eviction, root-CA exposure, upstream credential handling, single-instance enforcement. |
| `/code-health` | Readability, naming, in-file duplication, method size, parameter count, dead code, analyzer compliance (style and structure rules). |
| `/code-duplication` | Cross-file / cross-project duplication, parallel implementations, branch explosions, adapter accumulation, agent-induced fragmentation. |
| `/performance` | Allocations on the proxy hot path, blocking calls, sync-over-async, `System.IO.Pipelines` discipline, throttle accuracy, traffic-list virtualization, cache hit rates. |
| `/asynchrony` | `CancellationToken` propagation (`ATXTA008`), `async void`, ConfigureAwait, race conditions, stale captures, `ConcurrentDictionary` misuse, behavior-loop lifecycle. |
| `/quality-assurance` | TUnit conventions (`ATXTST002/003/004`), coverage gaps, hand-written stub quality, deterministic timing, parameterisation. |
| `/regression` | Change impact, public-contract drift, missing regression tests, event-pipeline ripple, high-risk surfaces. |
| `/serialization` | HAR 1.2 export/import, YAML round-trip, JSON content decoding, Protobuf / MessagePack, binary framing in HTTP/2 and SOCKS parsers. |
| `/avalonia` | AXAML correctness, binding safety, MVVM purity, threading marshalling, `Behavior<T>` lifecycle, virtualization, theme resources. |
| `/proxy-pipeline` | `Domain.Proxy` + `Framework.Networking` core path: `ProxyServer`, `SocketProxyListener`, `ConnectionDispatcher`, `IConnectionHandler` implementations, forward proxy outcomes. |
| `/transport-security` | TLS interception path, `TransportLayerSecurityInterceptorHandler`, ALPN negotiation, `LeafCertificateCache`, SNI proxying list, `CertificateAuthority`. |
| `/rule-engine` | `RuleEngine`, `IRuleRegistry`, request- and response-phase rules, `RequestPipelineAction` / `ResponsePipelineAction` discriminated unions, breakpoint inbox. |
| `/traffic-store` | `TrafficStore`, `WebSocketStore`, `ServerSentEventsStore`, `RemoteProcedureCallStore`, ring-buffer eviction, large-body spill, observable collection bridges. |
| `/scripting-sandbox` | `RoslynUserScriptCompiler`, `RoslynUserScript`, scriptable surfaces, ALC lifecycle, sandbox capabilities, timeouts and memory limits. |
| `/session-format` | HAR import/export pipeline under `Domain.Session/Har`, schema fidelity, custom `_proxyfan` extension fields, streaming write. |
| `/configuration` | `ConfigurationSnapshot`, `UserPreferences`, YAML migration, `IUserPreferencesStore`, hot-reload of mutable subsystems. |
| `/protocol-parsers` | HTTP/1.1, HTTP/2 (framing, HPACK, stream state machine), WebSocket, SSE, gRPC, SOCKS 4/5 parsers in `Framework.Networking`. |
| `/cli-automation` | `Cli` project: `System.CommandLine` handlers, headless proxy start, HAR summarisation, automation-friendly output. |
| `/product-manager` | Backlog coverage from `docs/BACKLOG.md`, missing user flows, edge cases that block a milestone. |
| `/user-experience` | Three-panel layout coherence, accessibility (WCAG 2.1 AA), keyboard navigation, error surfacing, theme switching. |
| `/triage` | Issue triage — reproduction, severity, ownership, attaching to a backlog item, deciding whether the matter is a bug, a feature, or a documentation gap. |
| `/devil-advocate` | Adversarial review — finds reversibility cliffs, load-bearing assumptions, leaked boundary types, premature abstractions. Read-only; no patches. |
| `/doctor` | Drives the repo from red to green when `Invoke-Build.ps1` (or `-RunTests`) is failing; triages, proposes a fix plan, branches, implements, re-verifies. |

## Sub-task template

```
Sub-task N: Use the /<skill> skill to <analyze the scope | review the following changes>.
Scope or diff: <SCOPE-or-DIFF>
```

When the diff is large, scope each sub-task to its relevant files using the
Focus column above. Whole-codebase analysis broadcasts the same scope to every
specialist.
