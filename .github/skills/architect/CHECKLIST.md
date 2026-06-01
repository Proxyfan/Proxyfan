# Architect checklist

Detailed reference for the `architect` skill, covering both whole-codebase
analysis and PR-diff review.

## Reference architecture

```
Clients (Client, Client.Desktop, Cli)
   └─► Presentation.*  (Shell, Traffic, Tools, …)
          └─► Domain.* (kernel + bounded contexts)
                 ▲
                 │
                 └─── Framework.* (Networking, Serialization, Platform, Extensibility, …)
```

The `DependencyInjection` project is the only seam where a `Domain.*`
abstraction and its `Framework.*` implementation are referenced together.

## Reference projects

- **Domain kernel** — `Domain`.
- **Domain contexts** — `Domain.Proxy`, `Domain.Traffic`, `Domain.Rules`,
  `Domain.Scripting`, `Domain.Certificates`, `Domain.Session`,
  `Domain.Throttling`, `Domain.Configuration`,
  `Domain.DomainNameSystemSpoofing`, `Domain.Updates`, `Domain.RemoteDevices`.
- **Framework adapters** — `Framework`, `Framework.Networking`,
  `Framework.Serialization`, `Framework.Platform`, `Framework.Extensibility`.
- **Plugin SPI** — `Plugin.Abstractions`.
- **Presentation** — `Presentation` and feature slices under
  `Presentation.*` (currently consolidated in the `Client` host).
- **Clients** — `Client`, `Client.Desktop`, `Cli`.

## Analysis

1. **Layer validation.** Confirm every project reference matches the layer
   table in `architecture.instructions.md`. Flag upward or sideways
   references (e.g. `Domain.Rules` referencing `Framework.Networking`).

2. **Dependency direction.** Confirm dependencies flow inward through the
   abstractions and never around them. Direct instantiation of a
   `Framework.*` concrete from a `Domain.*` type is a violation, even when
   the reference path technically exists in the .csproj.

3. **Circular dependencies.** Detect cycles between projects, namespaces, or
   types. ArchUnitNET conformance tests in `tests/*.Tests/Architecture/`
   should catch these — if a cycle exists and the conformance tests are
   green, that itself is a finding (the test is missing).

4. **Boundary integrity.** Flag leaking abstractions: implementation details
   surfaced across a layer boundary, shared mutable state crossing context
   lines, public types in `Domain.*` that expose `Framework.*` concretes in
   their signatures.

5. **Cohesion and coupling.** Flag classes that violate SRP, types with
   unrelated responsibilities, and types whose constructor parameter count
   keeps growing. Prefer a dedicated `*Dependencies` record (see existing
   examples in `Framework.Networking`) over arbitrary constructor expansion.

6. **Architectural drift.** Compare the actual structure against the declared
   architecture (MVVM, vertical slices in `Presentation.*`, event-driven
   communication between bounded contexts). Flag:
   - ViewModels referencing `Domain.*` mutable types instead of immutable
     projections.
   - Cross-bounded-context side effects implemented as direct method calls
     instead of `IDomainEventBus.Publish<T>`.
   - Code-behind (`.axaml.cs`) holding business or domain logic.
   - New `*Service` / `*Helper` / `*Util` types introduced beside an
     established sibling.

7. **Codebase-specific rules.** Flag:
   - Static methods declared on a non-static class (`ATXCS011`) — move to a
     dedicated `*static class*` partner.
   - `internal` access on a type that crosses an assembly boundary —
     internal types must not leak across `.csproj` lines via `InternalsVisibleTo`
     except for the matching `.Tests` project.
   - Primary constructors on classes (`ATXCS037` forbids them).
   - `params` keyword anywhere (`ATXCS055`).
   - Default parameter values anywhere (`ATXCS057`).
   - Inline `new T(…)` passed as an argument (`ATXCS058`) — assign to a
     named local first.
   - Empty `{}` collection / object initialiser (`ATXCS060`).
   - Use of LINQ in `src/` (Sonar `s6602` is at error severity).
   - Vague type suffixes (`Service`, `Helper`, `Util`, `Utils`,
     `Utilities`) — use precise vocabulary (`Manager`, `Repository`,
     `Provider`, `Factory`, `Handler`, `Processor`, `Aggregator`,
     `Dispatcher`, `Builder`, `Reader`, `Writer`, `Resolver`, `Validator`,
     `Coordinator`, `Executor`, `Calculator`, `Mapper`, `Converter`,
     `Formatter`, `Loader`, `Cache`, `Store`, `Engine`, `Pump`, `Pipeline`,
     `Scheduler`, `Registrar`).

## Bounded-context separation

- `Domain.<X>` may not reference `Domain.<Y>` except for the documented
  exceptions (`Domain.Session` → `Domain.Traffic`).
- Cross-context communication is **always** via `IDomainEventBus`.
- A new domain → domain dependency requires an ADR documented in the plan
  before implementation begins.

## Public-surface stability

- A change to a `public` type in `Domain.*` is a contract change. Confirm
  the change is intentional and update `docs/api/` via
  `.tools/Invoke-Build.ps1`.
- Adding a new public extensibility interface (`IContentDecoder`,
  `ITrafficInspector`, `IExportFormatter`, `IConnectionHandler`, …) is a
  Phase-3 stop-and-ask gate per `review-gates.instructions.md`.

## Forbidden silencers

When proposing a refactor, never recommend `#pragma warning disable`,
`[SuppressMessage]`, or `<NoWarn>` additions to clear an architectural
diagnostic. The path-scoped C# rules are the single source of truth for what
is permitted.
