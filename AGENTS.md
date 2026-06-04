# AGENTS.md

This file provides guidance to coding agents working with this repository. For full details see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/DESIGN.md](docs/DESIGN.md).

## Rules for Agents

- **CRITICAL: Never use `#pragma warning disable`**in `src/` or `tests/`. Fix root causes instead.

## Development Environment

- **Windows-only** — CI, builds, tests, and tooling all target Windows
- **PowerShell 7 (`pwsh`) only** — no bash, batch, Python, or legacy `powershell.exe`
- When a Bash tool must be used, invoke: `pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Some-Script.ps1`

## Build, Test, and Cleanup

| Script | Purpose | Key Flags |
|--------|---------|-----------|
| `.tools/Invoke-Build.ps1` | Build | `-RunTests`, `-Configuration`, `-SkipRestore` |
| `.tools/Run-Tests.ps1` | Run test suite | `-Configuration`, `-NoBuild` |
| `.tools/Invoke-Cleanup.ps1` | Code formatting (on-demand only) | — |
| `.tools/Initialize-Repository.ps1` | First-time setup | `-SkipWorkloads`, `-SkipTools`, `-RunTests` |

- Do **not** run `dotnet build` or `dotnet test` directly — use the scripts above
- Single test: `dotnet run --project tests/<Project> -- --filter "ClassName.MethodName"`

## Project Context

Proxyfan is an **HTTP debugging proxy** for inspecting, capturing, and modifying network traffic in real time. Built on .NET 10 with Avalonia UI and CommunityToolkit.Mvvm.

## Guiding Principles

- **Modular Monolith** — single deployable unit with explicit module boundaries; no microservices
- **Domain-Driven Design** — bounded contexts define module boundaries; domain logic isolated from infrastructure/UI
- **Vertical Slice Architecture** — features organized as self-contained slices cutting through all layers (each slice has its own subdirectory within each module)
- **Dependency Rule** — dependencies flow inward: `Presentation → Domain ← Framework`; domain never depends on infrastructure or UI
- **Platform Abstraction** — all OS-specific operations behind domain abstractions, implemented in `Framework.Platform`
- **Privacy by Default** — no telemetry, no external calls except user traffic and update checks; bodies never logged by default
- **Performance First** — `System.IO.Pipelines`, buffer pooling, async I/O; targets 10,000+ concurrent connections, 50,000+ req/min

## Solution Structure

### Domain Modules (business logic, no infrastructure dependencies)

| Module | Responsibility |
|--------|---------------|
| `Domain` (kernel) | Shared value objects, abstractions, base types (URL, HeaderCollection, ContentType, Result\<T\>, DomainError hierarchy) |
| `Domain.Proxy` | Proxy lifecycle, connection handling, tunneling |
| `Domain.Traffic` | Traffic capture, in-memory storage, filtering, querying |
| `Domain.Rules` | Rule engine — Allow/Block List, Map Local/Remote, Breakpoint, No Caching |
| `Domain.Scripting` | C# script compilation/execution via Roslyn with sandboxing |
| `Domain.Certificates` | Certificate generation, trust operations, SSL Proxying list |
| `Domain.Session` | Session persistence — save/load, HAR import/export |
| `Domain.Configuration` | YAML settings management |

### Framework Modules (infrastructure implementations)

| Module | Responsibility |
|--------|---------------|
| `Framework` | Shared utilities — buffer pooling, async helpers, observable collections |
| `Framework.Networking` | TCP listener, TLS interception, HTTP/1.1/2/WebSocket/gRPC/SSE/SOCKS parsing |
| `Framework.Serialization` | HAR 1.2, YAML, Protobuf, JSON, MessagePack, content decoding |
| `Framework.Platform` | Windows proxy registration, certificate store, process enumeration, registry, auto-update |

### Presentation Modules (UI)

| Module | Responsibility |
|--------|---------------|
| `Presentation` | Shared DI container accessor, ViewModel locator |
| `Presentation.Shell` | Main window, layout, menus, toolbar, status bar |
| `Presentation.Traffic` | Traffic list, inspector panels, content viewers |
| `Presentation.Tools` | Tool windows (Map Local/Remote, Breakpoint, Block/Allow Lists, Scripting Editor, Throttle, SSL Proxying, Certificate Manager) |

### Application Entry Points

| Module | Responsibility |
|--------|---------------|
| `Client` | Avalonia app host with module registration |
| `Client.Desktop` | Windows desktop entry point, single-instance enforcement, elevation |
| `Cli` | Headless CLI for automation (`System.CommandLine`) |
| `DependencyInjection` | Central DI registration |

## Dependency Rules

- **Domain modules** → depend only on `Domain` (kernel). `Domain.Session` also depends on `Domain.Traffic`.
- **Framework modules** → depend on `Domain` (kernel), their corresponding domain module, and `Framework`
- **Presentation modules** → depend on `Presentation`, `Domain` (kernel), and relevant domain modules
- **Forbidden**: Domain → Framework or Presentation; Presentation → Framework implementations; circular dependencies (enforced by ArchUnitNET)

## Bounded Contexts

| Context | Modules | Integration |
|---------|---------|-------------|
| Proxy | `Domain.Proxy`, `Framework.Networking` | Publishes: `ConnectionEstablished`, `RequestReceived`, `ResponseReceived`, `ConnectionClosed` |
| Traffic | `Domain.Traffic` | Consumes proxy events; publishes: `TrafficFlowCreated`, `TrafficFlowCompleted` |
| Rules | `Domain.Rules` | Consumes: `RequestReceived`, `ResponseReceived`; publishes: `RuleEvaluationResult` |
| Scripting | `Domain.Scripting` | Consumes: `RequestReceived`, `ResponseReceived`; publishes: `ScriptExecuted`, `RequestModified`, `ResponseModified` |
| Certificates | `Domain.Certificates`, `Framework.Platform` | Certificate lifecycle and trust operations |
| Session | `Domain.Session`, `Framework.Serialization` | Consumes: `TrafficFlowCompleted`; HAR import/export |
| Configuration | `Domain.Configuration`, `Framework.Serialization` | YAML settings, hot reload |

- **Domain Events** — in-process event bus for loose coupling between contexts
- **Shared Kernel** — `Domain` (kernel) provides common value objects
- **Anti-Corruption Layer** — Framework modules translate between domain and infrastructure

## Vertical Slice Convention

Each feature is a self-contained slice with its own subdirectory within each module. Slices communicate only through the domain event bus or shared abstractions in the Domain kernel. Slices never directly reference internal types of other slices.

## Rule Evaluation Order (fixed, short-circuits on match)

**Request phase:**
1. **Allow List** — if active and host not matched → connection closed, remaining rules skipped
2. **Block List** — if matched → request rejected (403), remaining rules skipped
3. **Map Remote** — URL rewritten; modified request continues through pipeline
4. **Map Local** — local response returned immediately; request NOT forwarded; response-phase rules still execute
5. **Breakpoint** — request paused for user editing; after resume, continues through pipeline
6. **Script** — `OnRequest` executes; may modify request or return mock response (short-circuits server call)
7. **No Caching** — strips cache headers from request

**Response phase:**
1. **No Caching** — strips cache headers from response
2. **Script** — `OnResponse` executes; may modify response
3. **Breakpoint** — response paused for user editing

**Key feature interactions:**
- Allow List + Block List → Allow List first; if allowed, Block List still applies (Block wins for specific host)
- Map Remote + Map Local → Map Remote rewrites URL first; if rewritten URL matches Map Local, local response served
- Map Local + Breakpoint/Script → response-phase rules still fire on the local response
- Block List + anything → Block List short-circuits; no further rules execute

## Cross-Cutting Concerns

### Dependency Injection
- **Container**: `Microsoft.Extensions.DependencyInjection` via `IHostBuilder`
- **Domain Services** — Scoped or Singleton; **Framework** — Singleton (stateless); **ViewModels** — Transient (one per view, resolved via `ViewModelLocator`); **Configuration** — Singleton via `IOptions<T>`

### Error Handling
- Domain operations return `Result<T>` (with `Value`, `Error`, `IsSuccess`) instead of throwing exceptions
- `DomainError` hierarchy: `ProxyError`, `CertificateError`, `RuleError`, `ScriptError`, `SessionError`, `ConfigurationError`
- Each error has `Message`, `Code` (e.g., `"PROXY_CONNECTION_TIMEOUT"`), and optional `InnerException`
- Proxy pipeline errors fail individual connections gracefully — never crash the app
- UI errors shown via toast/status bar, never modal dialogs
- Rule/script errors do NOT crash pipeline — traffic falls through to next rule or server

### Configuration
- **Precedence** (highest → lowest): CLI arguments → environment variables (`PROXYFAN_*`) → `%LOCALAPPDATA%\Proxyfan\config.kv` → `defaults.yaml` (shipped)
- Hot reload via file watcher (non-proxy settings); proxy settings (port, upstream) require restart
- Strongly-typed options via `Microsoft.Extensions.Options` with key=value backing store

### Logging & Privacy
- `Microsoft.Extensions.Logging` with structured logging to `%LOCALAPPDATA%\Proxyfan\logs\`
- Request/response bodies **never** logged; headers only at Trace level; `Authorization`, `Cookie`, `Set-Cookie` redacted by default

### Internationalization
- All user-visible strings in `.resx` files; naming: `{Feature}_{Context}_{Element}`
- Locale resolution: user config → Windows system locale → `en-US` fallback
- Switching takes effect immediately without restart
- NOT localized: logs, CLI output, error codes, config key names

## MVVM & UI Architecture

- `CommunityToolkit.Mvvm` with `[ObservableProperty]`, `[RelayCommand]`, compiled bindings
- ViewModel communication via `IMessenger` (`WeakReferenceMessenger`); VMs never reference other VMs directly
- VMs are Transient (each view gets its own instance), implement `IDisposable`, disposed by view unload handler
- Three-panel layout: Source List (left) | Traffic Flow List (center) | Inspector Panel (right)
- Themes: Light (default), Dark, System — runtime switching without restart
- Accessibility: WCAG 2.1 Level AA, full keyboard navigation, screen reader support

## Networking Architecture

- **Proxy engine** built from scratch: raw TCP sockets + `System.IO.Pipelines`
- **Components**: TCP Listener → Connection Dispatcher (detects HTTP/SOCKS) → Pipeline Executor (Throttle → Rules → Forward → Record)
- **TLS interception** (MITM): on CONNECT, checks SSL Proxying List → generates leaf cert signed by root CA → bidirectional TLS handshake → decrypted relay; disabled domains get raw TCP tunnel
- **Protocol handlers** (all implement `IConnectionHandler`): HTTP/1.1, HTTP/2, HTTPS (CONNECT), WebSocket, gRPC (over HTTP/2), SSE, SOCKS4/5, Streaming HTTP
- **Upstream proxy chaining**: HTTP/HTTPS/SOCKS/PAC support with bypass list and basic auth
- **Network throttling**: token bucket algorithm; presets: 2G, 3G, 4G, WiFi, Slow/Bad Network, 100% Loss; per-connection scope

## Persistence & Storage

- **In-memory traffic store**: concurrent ring buffer, default 10,000 flows (configurable to 100,000+); oldest evicted on capacity; single-writer via `Channel<T>`, lock-free reads
- **Large bodies** (>1 MB): spilled to temp files; memory pressure strategy: soft (>70%) → warning, hard (>90%) → evict 10%, critical (OS pressure) → pause capture
- **HAR 1.2**: save/load sessions; streaming writes for large sessions; optional gzip (`.har.gz`); custom `_proxyfan` namespace for color tags/comments
- **Key-value config**: `%LOCALAPPDATA%\Proxyfan\config.kv` with schema validation and hot reload
- **Certificates**: root CA + custom certs in `%LOCALAPPDATA%\Proxyfan\certificates\`; private keys encrypted via Windows DPAPI; leaf cert LRU cache (default 1,000 entries)

## Extensibility Interfaces

- **`IContentDecoder`** — content rendering (built-in: JSON, XML, HTML, Protobuf, MessagePack, form data, images, hex, GraphQL)
- **`ITrafficInspector`** — inspector tabs with `DisplayName`, `Order`, `CanInspect`, `CreateViewModel` (built-in: Headers, Body, Query, Cookies, Auth, Raw, Timing, Summary)
- **`IExportFormatter`** — export formats (built-in: HAR 1.2, cURL, Raw HTTP, JSON)
- All registered in DI; extensible by implementing the interface and registering

## Scripting Constraints

- C# scripting via Roslyn; `OnRequest`/`OnResponse` methods; `sharedState` dict scoped per-flow per-script
- **Sandboxing**: separate `AssemblyLoadContext`; no file system/network/reflection emit/threading access
- **Limits**: memory default 50 MB (10–500 MB), timeout default 5 sec (1–60 sec)
- **Errors**: compilation → script invalid, traffic unmodified; runtime exception → caught, traffic unmodified, logged; timeout → cancelled, unmodified; OOM → ALC unloaded, script auto-disabled

## Performance Targets

| Metric | Target |
|--------|--------|
| Proxy startup | < 1 second |
| Request latency overhead | < 1 ms (excluding rules/throttling) |
| Concurrent connections | 10,000+ |
| Requests per minute | 50,000+ |
| Traffic list scrolling | 100,000+ flows smooth |
| Memory (idle / 10K flows) | < 100 MB / < 500 MB |
| Session save/load (10K flows) | < 5 seconds each |
| Script compilation | < 2 seconds |

## Memory & Buffer Defaults

| Setting | Default | Range |
|---------|---------|-------|
| `capture.maxFlows` | 10,000 | 100–1,000,000 |
| `capture.largeBodyThreshold` | 1 MB | 64 KB–100 MB |
| WebSocket/gRPC message buffer | 1,000/connection | configurable |
| SSE event buffer | 5,000/connection | configurable |
| Streaming protocol global budget | 200 MB | configurable |
| `advanced.certificateCacheSize` | 1,000 | 10–100,000 |

## Key Configuration Settings

| Setting | Default | Valid Range |
|---------|---------|-------------|
| `proxy.port` | 8080 | 1024–65535 |
| `ui.fontSize` | `medium` | `small`/`medium`/`large` or 8–72 pt |
| `session.autoSaveInterval` | 300 sec | 0 (disabled) or 60–86,400 |
| `updates.checkInterval` | 86,400 sec | 3,600–604,800 |
| `privacy.logLevel` | `information` | `trace`/`debug`/`information`/`warning`/`error` |
| `privacy.logRetentionDays` | 30 | 1–365 |
| `advanced.bufferSize` | 64 KB | 4 KB–1 MB |

## Roslyn Linting Rules (enforced as errors)

- **IDE0022**: Block body required for methods (no `=>` arrow methods with parameters; properties are fine)
- **IDE0045**: Use `if/else` instead of ternary for assignments
- **IDE0046**: Use `if/else` instead of ternary for returns
- **S121**: All `if`/`else` branches must use curly braces

## Test Suite

- **TUnit** with **Microsoft.Testing.Platform** runner (not xUnit/NUnit); one test project per source project under `tests/`
- **Naming**: file `{ClassUnderTest}Tests.cs`, class `{ClassUnderTest}Tests` (or `{ClassUnderTest}{Qualifier}Tests`), method `{Method}_{Scenario}_{ExpectedResult}`
- **Stubs**: hand-written in `Stubs/` — mocking frameworks forbidden
- **Shared state**: `[NotInParallel]` on classes that mutate shared state
- **Test data**: `TestData/` subdirectories; factories in `Helpers/` (e.g., `TrafficFlowFactory.CreateValid()`)
- **Common assertions**: `IsEqualTo`, `IsTrue`, `IsFalse`, `IsNotNull`, `IsNull`, `Count().IsEqualTo(n)`, `Throws<T>`, `IsSameReferenceAs`
- **Architecture tests**: ArchUnitNET (`TngTech.ArchUnitNET.TUnit`) enforces dependency rules, naming conventions, no circular dependencies
- **Coverage target**: minimum 80% per module (line + branch)

## Key Dependencies

| Package | Use |
|---------|-----|
| Avalonia 11.3.x | Cross-platform UI framework |
| CommunityToolkit.Mvvm 8.4.x | MVVM source generators |
| Microsoft.CodeAnalysis.CSharp.Scripting | Roslyn C# scripting |
| YamlDotNet | YAML configuration |
| System.CommandLine | CLI parser |
| TUnit 1.12.x | Testing framework |
| TngTech.ArchUnitNET 0.13.x | Architecture conformance tests |
| SonarAnalyzer.CSharp 10.19.x | Static analysis (enforced as errors) |
