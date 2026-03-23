# Proxyfan

HTTP debugging proxy for inspecting, capturing, and modifying network traffic in real time on **Windows**.

> **Platform:** Proxyfan targets **Windows 10 and later** exclusively. The architecture supports
> future cross-platform expansion, but macOS and Linux are not on the current roadmap.

> **Development Status:** Proxyfan is currently under development.

## Overview

Proxyfan is a desktop application for built with .NET. It sits between your applications and the internet, allowing you to see every HTTP and HTTPS request, inspect headers and bodies, modify traffic on the fly, and simulate network conditions.

## Key Features

- **Traffic Capture** — Intercept HTTP/1.1 and HTTPS traffic with automatic system proxy registration
- **HTTPS Decryption** — Man-in-the-middle TLS interception with per-domain control via the SSL Proxying List
- **Traffic Inspection** — Headers, body (JSON, XML, HTML, images, hex), raw view, and timing breakdown
- **Traffic Filtering** — Real-time filter bar with support for status codes, content types, methods, and text search
- **Map Local** — Serve responses from local files instead of the remote server
- **Map Remote** — Rewrite request URLs to redirect traffic to a different server
- **Breakpoints** — Pause requests and responses mid-flight to inspect and edit before forwarding
- **Scripting (C#)** — Write C# scripts with full Roslyn support to programmatically modify traffic
- **Block / Allow Lists** — Control which domains are captured or blocked
- **Network Throttling** — Simulate slow networks (3G, LTE, Satellite, etc.) with bandwidth limits and latency injection
- **Session Management** — Save and load sessions in HAR 1.2 format
- **Internationalization** — All user-visible text externalized to resource files for localization
- **WebSocket, gRPC, SSE** — Inspect advanced protocol traffic (planned for v2.0)
- **CLI Mode** — Headless proxy for CI/CD and automation (planned for v2.0)

## Architecture

Proxyfan follows a **modular monolith** architecture with domain-driven design boundaries:

- **Domain Layer** — Core business logic organized into bounded contexts (Proxy, Traffic, Rules, Scripting, Certificates, Session, Configuration)
- **Framework Layer** — Infrastructure concerns (Networking, Serialization, Platform abstraction)
- **Presentation Layer** — MVVM-based UI with Avalonia and CommunityToolkit.Mvvm
- **Client Layer** — Application hosting and dependency injection

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dot.net/download) (version 10.0.104)
- Windows 10 or later
- PowerShell 7 (`pwsh`)

### First-Time Setup

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-Repository.ps1
```

This installs required workloads, restores packages, and builds the solution.

### Build

```powershell
# Standard Debug build (also regenerates docs/api/)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build and run tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Incremental build (skip package restore)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore
```

### Test

```powershell
# Full test suite
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1

# Tests only (skip rebuild)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -NoBuild
```

### Run

```powershell
dotnet run --project src/Clients/Client.Desktop
```

## Technology Stack

| Technology | Purpose |
|------------|---------|
| .NET 10 / C# 13 | Runtime and language |
| Avalonia 11.3 | Cross-platform UI framework |
| CommunityToolkit.Mvvm 8.4 | MVVM source generators |
| System.IO.Pipelines | High-performance proxy I/O |
| TUnit 1.12 | Testing framework |
| ArchUnitNET 0.13 | Architecture conformance tests |
| SonarAnalyzer.CSharp | Static analysis (enforced as errors) |

## License

See [LICENSE](LICENSE) for details.
