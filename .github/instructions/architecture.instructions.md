---
applyTo: "src/**/*.cs,tests/**/*.cs"
---

# Architecture rules

Proxyfan is a **modular monolith** with strict dependency direction. Every C# file
in `src/` or `tests/` must respect the layer boundaries listed below. Boundary
violations are detected at build time by ArchUnitNET conformance tests
(`tests/*.Tests/Architecture/`); fix them at the source, never by editing the
conformance tests.

## Layers and direction

```
Clients (Client, Client.Desktop, Cli)
   └─► Presentation.*
          └─► Domain.* (kernel + bounded contexts)
                 ▲
                 │
                 └─── Framework.* (Networking, Serialization, Platform, …)
```

| From | May depend on | Forbidden |
|---|---|---|
| `Domain` (kernel) | Nothing in this solution | Every other project |
| `Domain.<Context>` | `Domain` (kernel) and the sibling listed in `AGENTS.md` | Framework, Presentation, Client, Cli |
| `Framework` | `Domain` (kernel) only | Any `Framework.<X>`, Presentation, Client |
| `Framework.<X>` | `Framework`, `Domain` (kernel), the matching `Domain.<X>` | Other `Framework.<X>`, Presentation, Client |
| `Presentation` | `Domain` (kernel), `Domain.<X>` | `Framework.<X>` concretes |
| `Presentation.<X>` | `Presentation`, `Domain` (kernel), `Domain.<X>` | `Framework.<X>` concretes |
| `Client` | `Presentation.*`, `Domain.*`, `Plugin.Abstractions` | Direct types from inside `Framework.*` |
| `Client.Desktop` | `Client`, `Framework.Platform` (Windows host code only here) | Domain mutations, business logic |
| `Cli` | `Domain.*`, `Framework.*` (via DI), `Plugin.Abstractions` | Avalonia / Presentation types |

`DependencyInjection` is the wiring layer that registers concrete `Framework.*`
implementations behind `Domain.*` abstractions. It is the only place where the
domain abstraction and its framework implementation are referenced together.

## Module responsibilities

### Domain kernel (`Domain`)
Cross-context primitives only: `Result<T>`, `VoidResult`, `DomainError` and
descendants, `IDomainEventBus`, `DomainEventHandler<T>`, `IDomainEvent`. Do not
add anything specific to a bounded context here.

### Domain contexts (no `Framework.*` references)
- `Domain.Proxy` — proxy lifecycle, connection acceptance, reverse-proxy routing,
  `IConnectionDispatcher`, `IProxyListener`, `ProxyServer`.
- `Domain.Traffic` — captured flow types, in-memory stores
  (`TrafficStore`, `WebSocketStore`, `ServerSentEventsStore`,
  `RemoteProcedureCallStore`), composer history, parsers for cookies / cache-control
  / content-type / form data, request composer, formatters.
- `Domain.Rules` — `IRuleEngine`, `IRuleRegistry`, request- and response-phase
  rules (`AllowListRule`, `BlockListRule`, `MapLocalRule`, `MapRemoteRule`,
  `BreakpointPause`, `NoCachingRule`, …), `RequestPipelineAction` /
  `ResponsePipelineAction` discriminated unions.
- `Domain.Scripting` — `IUserScript`, `IUserScriptCompiler`, `RoslynUserScript`,
  `RoslynUserScriptCompiler`, scriptable surfaces, mutable scripting configuration.
- `Domain.Certificates` — `ICertificateGenerator`, `ICertificateCache`,
  `ICertificateStore`, `CertificateAuthority`, `LeafCertificateCache`, SNI proxying
  list, provisioning helpers.
- `Domain.Session` — HAR import/export contracts; depends on `Domain.Traffic`
  (the only domain → domain dependency).
- `Domain.Throttling` — `TokenBucket`, throttle profiles and presets,
  `ThrottledStreamWriter`.
- `Domain.Configuration` — YAML-backed snapshot, user preferences, migration.
- `Domain.DomainNameSystemSpoofing` — local DNS override entries.
- `Domain.Updates` — auto-update version checks.
- `Domain.RemoteDevices` — remote-device proxying configuration.

### Framework adapters
- `Framework` — buffer pools, async helpers, observable collections, shared utilities.
- `Framework.Networking` — TCP listener (`SocketProxyListener`), connection
  dispatcher, all HTTP/1.1, HTTP/2 (HPACK, framing, streams), CONNECT tunnelling,
  TLS interception, WebSocket, SSE, gRPC, SOCKS 4/5 protocol handlers, throttle
  applier, request repeater.
- `Framework.Serialization` — HAR readers/writers, YAML, JSON, MessagePack,
  Protobuf, content decoders.
- `Framework.Platform` — Windows-specific surfaces: system proxy registration,
  certificate store interop, process enumeration, DPAPI key protection,
  registry, auto-update integration.
- `Framework.Extensibility` — runtime registration helpers for the
  `IContentDecoder`, `ITrafficInspector`, `IExportFormatter` extension points.

### Presentation
- `Presentation` — shared infrastructure: ViewModel locator,
  dialog/overlay/messaging plumbing, accessor for the DI container.
- Each `Presentation.<X>` slice owns its ViewModels, Views, and converters for
  one feature surface (Shell, Traffic, Tools, …). Slices communicate exclusively
  through `IMessenger` or domain abstractions; one ViewModel never references
  another ViewModel concretely.

### Clients
- `Client` — Avalonia app host, App.axaml resources, top-level startup,
  module registration via the `HostExtensions` chain.
- `Client.Desktop` — Windows entry point (`Program.Main`), single-instance
  enforcement, elevation prompts.
- `Cli` — `System.CommandLine`-driven headless mode for automation, HAR
  summarisation, and proxy startup without a UI.

## Cross-context communication

Cross-context side effects flow through `IDomainEventBus` (`Publish<T>` is
synchronous, fire-and-forget, exception-isolated). Direct calls from one bounded
context's domain types to another are forbidden. The bus contract is:

- Publishers raise events through `IDomainEventBus.Publish<TEvent>`. Subscribers
  register `DomainEventHandler<TEvent>` and dispose the returned `IDisposable`
  to unsubscribe.
- Handler exceptions are caught by the bus and never block siblings — design
  handlers to be idempotent and side-effect-local.
- Events live in the publishing context's `Events/` subdirectory and end in
  `Changed`, `Recorded`, `Created`, `Closed`, or a similarly past-tense verb.

## Result and error handling

Domain operations that can fail return `Result<T>` or `VoidResult` instead of
throwing. The discriminator is `IsSuccess`; on failure, `Error` is a
`DomainError` carrying `Code`, `Message`, and optional `InnerException`. Each
bounded context defines its own `DomainError` subclass:

| Context | Base error |
|---|---|
| `Domain.Proxy` | `ProxyError`, `ProxyAlreadyRunningError`, `ProxyBindError`, `ProxyFaultedError`, `ProxyNotRunningError` |
| `Domain.Rules` | `RuleError` |
| `Domain.Scripting` | `ScriptError` |
| `Domain.Certificates` | `CertificateError` |
| `Domain.Session` | `SessionError` |
| `Domain.Configuration` | (configuration error types under `Migration/`) |

Exceptions still surface at the runtime boundary (socket failures, OS errors,
Roslyn compilation crashes). Always translate them into a `DomainError` before
returning to the caller; never let raw exceptions escape a public domain method.

## Vertical slices

Each Presentation feature is a self-contained slice (e.g.
`Presentation.Traffic`, `Presentation.Tools`) with its own `ViewModels/` and
`Views/`. Slices never reach into another slice's internals — coordination goes
through messaging (`IMessenger`) or a shared domain abstraction.

## Dependency injection

- Container: `Microsoft.Extensions.DependencyInjection` hosted via
  `Microsoft.Extensions.Hosting`.
- Lifetimes:
  - Domain services that hold state for the running proxy: **Singleton**.
  - Framework services (parsers, generators, factories): **Singleton** (stateless).
  - ViewModels: **Transient** — one per view, resolved through the locator.
  - Options bound via `Microsoft.Extensions.Options`: **Singleton** through
    `IOptions<T>`; **Scoped** through `IOptionsSnapshot<T>` when hot reload is needed.
- Registrations live in `DependencyInjection/ServiceCollectionExtensions.cs` and
  per-module extension methods. Never register concrete types in Presentation
  code-behind.

## Suffix vocabulary

Use precise, behaviour-bearing suffixes — `Manager`, `Repository`, `Provider`,
`Factory`, `Handler`, `Processor`, `Scheduler`, `Aggregator`, `Dispatcher`,
`Registrar`, `Builder`, `Reader`, `Writer`, `Resolver`, `Validator`,
`Coordinator`, `Executor`, `Calculator`, `Mapper`, `Converter`, `Formatter`,
`Loader`, `Cache`, `Store`, `Engine`, `Pump`, `Pipeline`. Avoid the catch-all
`Service` and `Util` / `Utils` / `Utilities`.

## Public surface and XML docs

Every `public` and `protected` member requires an XML doc comment — `CS1591` is
treated as an error. Document `<summary>`, `<param>`, `<returns>`, `<exception>`,
and `<typeparam>` exhaustively. The doc XML feeds `docs/api/`.
