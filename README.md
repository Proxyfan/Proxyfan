# Proxyfan

> **An open-source HTTP debugging proxy for Windows — a free alternative to Charles, Fiddler, and Burp Suite.**

[![CI](https://github.com/Proxyfan/Proxyfan/actions/workflows/ci.yml/badge.svg)](https://github.com/Proxyfan/Proxyfan/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dot.net)

Proxyfan sits between your applications and the internet so you can see every HTTP and
HTTPS request, inspect headers and bodies, modify traffic on the fly, simulate poor
network conditions, replay requests, and capture everything to disk.

> **Platform:** Proxyfan currently targets **Windows 10 and later**. The architecture
> supports future cross-platform expansion via Avalonia; macOS and Linux are not yet on
> the roadmap.
>
> **Status:** Active development. Most features below are complete and ship-ready; the
> remaining items are listed under [Roadmap](#roadmap).

## Why Proxyfan?

| Capability | Proxyfan | Charles | Fiddler Classic | mitmproxy |
| --- | :---: | :---: | :---: | :---: |
| Cost | Free, MIT | Paid | Free* | Free, MIT |
| Open source | ✅ | ❌ | ❌ | ✅ |
| Native Windows UI | ✅ | ✅ (Java) | ✅ | ❌ (CLI/web) |
| HTTPS interception (per-host opt-in) | ✅ | ✅ | ✅ | ✅ |
| WebSocket inspector | ✅ | ✅ | ❌ | ✅ |
| gRPC inspector | ✅ | ✅ | ❌ | ❌ |
| C# scripting (Roslyn) | ✅ | ❌ | ✅ (FiddlerScript) | Python |
| Request composer + history | ✅ | ✅ | ✅ | ❌ |
| Map Local / Map Remote | ✅ | ✅ | ✅ | ❌ |
| Breakpoints (request + response) | ✅ | ✅ | ✅ | ✅ |
| Network throttling presets | ✅ | ✅ | ❌ | ❌ |
| Reverse proxy (port-based routing) | ✅ | ❌ | ❌ | ✅ |
| Plugin system (isolated ALC) | ✅ | ❌ | ❌ | ❌ |
| DNS spoofing | ✅ | ❌ | ❌ | ✅ |
| Auto-update | ✅ | ✅ | ✅ | ❌ |
| Telemetry-free, privacy-first | ✅ | ❌ | ❌ | ✅ |
| HAR 1.2 import / export | ✅ | ✅ | ✅ | ✅ |
| CLI for CI/CD | ✅ | ❌ | ❌ | ✅ |

\* Fiddler Classic is freeware but closed source.

## Features

### Traffic capture & inspection

- **System proxy registration** — one click to start intercepting all Windows HTTP traffic
- **HTTPS decryption** — MITM TLS interception with per-domain control via the SSL Proxying List
- **Inspector tabs** — Headers, Body, Query, GraphQL, Cookies, Authorization, Raw, Summary, Timing waterfall
- **Body decoders** — JSON pretty-print, XML, HTML, images (preview), Protobuf, MessagePack, form-encoded, hex dump
- **Real-time filter bar** — instant text + status code + method + content-type filtering with `Ctrl+F`
- **Color tags + comments** — annotate flows for easy navigation
- **Custom columns** — add header-key columns derived from request or response
- **Source grouping** — left-pane source list groups flows by host

### Traffic modification

- **Map Local** — serve canned local responses (with status, headers, body) for any URL pattern
- **Map Remote** — rewrite request URLs to redirect traffic to a different scheme/host/port/path
- **Breakpoints** — pause request and response phases mid-flight for live editing
- **Block / Allow Lists** — wildcard / exact / regex matching, short-circuits the pipeline
- **No-Caching rule** — strip cache headers from requests and responses (`Ctrl+Shift+N`)
- **C# scripting (Roslyn)** — write `OnRequest` and `OnResponse` handlers with full BCL access; sandboxed in an isolated `AssemblyLoadContext` with memory + timeout limits

### Network simulation

- **Throttle profiles** — built-in 2G, 3G, 4G, WiFi, Slow / Bad Network, 100% Loss presets
- **Per-direction bandwidth + latency + packet loss** — enforced per-connection
- **DNS spoofing** — override hostname → IP mappings without editing the system hosts file

### Advanced protocols

- **WebSocket inspector** — direction-tagged message timeline, opcode + size, JSON / hex preview, direction + content-type filters
- **HTTP/2 native orchestration** — when both ends negotiate `h2` via ALPN the proxy runs a frame-relay orchestrator that preserves HPACK end-to-end while parsing HEADERS, CONTINUATION, DATA, and RST_STREAM into the same inspector view as HTTP/1.1 flows. HTTP/1.1 fallback remains automatic for clients/servers that don't negotiate `h2`.
- **Server-Sent Events inspector** — chronological event list per SSE flow with event-type filter, live updates as new events arrive, full event detail (type, id, retry hint, data) in the side panel
- **gRPC inspector** — chronological message timeline per gRPC stream with direction filter (Outbound / Inbound / All), per-message compression flag, hex-dump payload viewer, and live status text ("streaming" / "closed"). Auto-detects `application/grpc` (and `application/grpc+proto` / `+json` / `-web`) on HTTP/2 responses; extracts length-prefixed messages in both directions. Decodes uncompressed payloads as a structured protobuf field tree (schema-less by default; field names + enum value labels when a matching `FileDescriptorSet` is loaded via **Tools → gRPC Descriptors...**).
- **Reverse proxy** — define routes (listen port → backend host:port + TLS mode) with periodic health probing

### Productivity

- **Request Composer** — build, send, and re-send ad-hoc HTTP requests with history, search, star
- **Diff Tool** — compare two captured flows side-by-side (request body / response body / headers)
- **Repeat selected** — replay any captured request 1× or 10×
- **Copy as cURL / Copy URL / Copy as Raw HTTP** — right-click any captured flow to copy a reproducible representation to the clipboard
- **Export to cURL** — copy any request as a cURL command line
- **Session save / load** — HAR 1.2 format (interoperates with Charles, Chrome DevTools, etc.)
- **Configurable keyboard shortcuts** — every action rebindable with conflict detection; persisted to `%LOCALAPPDATA%\Proxyfan\shortcuts.json`

### Operations

- **Plugin system** — drop-in C# plugins loaded into isolated `AssemblyLoadContext` instances with hot reload, marketplace update checks, enable/disable per plugin
- **Auto-update** — periodic GitHub Releases poll, in-app banner with version + changelog
- **Configuration migration** — older `config.yaml` files are migrated forward at startup with backups
- **Internationalization** — all user-visible text in `.resx`; locale follows Windows by default
- **Themes** — Light, Dark, follow-System with runtime switching
- **Accessibility** — `AutomationProperties.Name` on every interactive control; screen-reader friendly
- **Privacy by default** — no telemetry, no external calls except user traffic and the update check; bodies never logged

### Headless / CLI mode

- `proxyfan-cli start --port N [--duration N] [--output capture.har]` — start the proxy server headlessly, optionally auto-stop after N seconds, and export captured flows to HAR on shutdown
- `proxyfan-cli har-summary <file>` — human-readable summary of a HAR capture
- `proxyfan-cli har-to-curl <file>` — emit a cURL command per captured request
- `proxyfan-cli har-filter --input <in> --output <out> --pattern <glob>` — CI/CD slicing
- `proxyfan-cli send --method POST --url ... --header "Accept: application/json" --body "..."` — one-off HTTP request
- `proxyfan-cli help` — full command list

## Install

### Portable ZIP (recommended for trying out Proxyfan)

1. Download the latest `Proxyfan-portable-<version>-win-x64.zip` from
   [Releases](https://github.com/Proxyfan/Proxyfan/releases).
2. Extract anywhere.
3. Run `Client.Desktop.exe`.

The portable build is fully self-contained — no .NET runtime install required.

### MSI installer (enterprise / Group Policy)

1. Download `Proxyfan-<version>-win-x64.msi` from
   [Releases](https://github.com/Proxyfan/Proxyfan/releases).
2. Run the installer (admin elevation required for per-machine install).
3. Launch Proxyfan from the Start menu.

The MSI is built with WiX 5 from [`installer/Proxyfan.wxs`](installer/Proxyfan.wxs).
Pass `-BuildMsi` to `.tools/Build-Installer.ps1` to produce one locally
(requires `dotnet tool install --global wix`).

### Build from source

```powershell
# First-time setup (installs workloads, restores packages)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-Repository.ps1

# Standard Debug build (regenerates docs/api/)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build + tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Portable ZIP build
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1 -Version 1.0.0

# Portable ZIP + MSI installer (requires `dotnet tool install --global wix`)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Build-Installer.ps1 -Version 1.0.0 -BuildMsi

# Run
dotnet run --project src/Clients/Client.Desktop
```

## Architecture

Proxyfan follows a **modular monolith** architecture with **domain-driven design**
boundaries and **vertical slice** feature organization.

- **Domain Layer** — Core business logic organized into bounded contexts (Proxy, Traffic, Rules, Scripting, Certificates, Session, Configuration, Updates, RemoteDevices, DomainNameSystemSpoofing, Throttling)
- **Framework Layer** — Infrastructure concerns (Networking — raw sockets + `System.IO.Pipelines`; Serialization — HAR/JSON/Protobuf/MessagePack; Platform — Windows certificate store, system proxy registration, registry; Extensibility — plugin loader)
- **Presentation Layer** — Avalonia + CommunityToolkit.Mvvm + shortcuts + theming + localization
- **Client Layer** — Application hosting, dependency injection, tool window opener

Detailed design: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/DESIGN.md](docs/DESIGN.md), [docs/BACKLOG.md](docs/BACKLOG.md).

## Performance

| Metric | Target |
| --- | --- |
| Proxy startup | < 1 second |
| Request latency overhead | < 1 ms (excluding rules / throttling) |
| Concurrent connections | 10,000+ |
| Requests per minute | 50,000+ |
| Traffic list scrolling | 100,000+ flows smooth |
| Memory (idle / 10K flows) | < 100 MB / < 500 MB |

## Privacy

- **No telemetry.** Proxyfan never phones home.
- **No external calls.** The only outbound traffic is user-captured traffic (forwarded to its real destination) and the periodic GitHub Releases poll for updates.
- **Bodies never logged.** Headers are logged only at Trace level. `Authorization`, `Cookie`, and `Set-Cookie` are redacted by default.

## Roadmap

- **MSIX installer for Microsoft Store distribution** — the MSI installer covers
  enterprise / Group Policy deployment; an MSIX package for Microsoft Store
  submission is on the v2.x roadmap and requires Windows SDK tooling.

## Roslyn analyzer rules

Proxyfan builds with `TreatWarningsAsErrors=true` and the following project-wide rules are enforced as errors:

- All analyzer diagnostics (no `#pragma warning disable` in `src/` or `tests/`)
- `IDE0022` — methods use block bodies (no `=>` arrow methods with parameters)
- `IDE0045` / `IDE0046` — use `if/else` instead of ternary for assignments and returns
- `S121` — all `if`/`else` branches use curly braces
- Automaticks analyzers (alphabetical using order, no LINQ, naming, member ordering, etc.)

## Test suite

- **TUnit** with **Microsoft.Testing.Platform**; one test project per source project under `tests/`
- **ArchUnitNET** for architecture conformance (dependency rules, naming, no circular dependencies)
- **AppAccessibilityArchitectureTests** — scans every `.axaml` for unlabelled interactive controls
- **Hand-written stubs** in `Stubs/` subdirectories — no mocking frameworks
- Current coverage: **97.5% line**, **94.8% branch**, **99.3% method** (every assembly ≥ 90% line and ≥ 90% branch).

## Contributing

See [AGENTS.md](AGENTS.md) for the development environment rules and the architecture overview.

## License

[MIT](LICENSE).
