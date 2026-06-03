# Copilot Instructions — Proxyfan

Proxyfan is a Windows HTTP debugging proxy that captures, inspects, and rewrites
network traffic in real time. The codebase is .NET 10, Avalonia, CommunityToolkit.Mvvm,
TUnit, and Roslyn scripting, organised as a modular monolith with strict
domain / framework / presentation layering.

## Quality bar

These standards apply to every plan, refactor, and PR — including autopilot runs.

**Rewarded:** simplicity, reusing existing abstractions, scalable + allocation-aware
design, ≥ 80 % line + branch coverage on changed modules, explicit privacy posture.

**Penalised:** speculative abstractions, parallel implementations beside an existing
helper, unguarded LINQ in hot paths, suppressing analyzers instead of fixing the
root cause, anything that captures request or response bodies into logs.

**Prerequisites — confirm before proposing or implementing:** the change's
*scope* (which module owns it), *blast radius* (which downstream contexts react via
`IDomainEventBus`), and *reversibility* (whether the change touches a `Result<T>`
public contract, a `HypertextTransferProtocolForwarder`-style pipeline seam, the
HAR-on-disk shape, the YAML config schema, or any extensibility interface in
`Framework.Extensibility` / `Plugin.Abstractions`).

**CRITICAL — ADR-or-revise:** any change that adds a new module/project, a new
extensibility interface (`IContentDecoder`, `ITrafficInspector`, `IExportFormatter`,
`IConnectionHandler`, …), a new package in `Directory.Packages.props`, or a new
public contract on a domain service must include a one-paragraph ADR in the plan
covering **Context → Decision → Alternatives rejected → Reversal path in six months**.
If you cannot describe the reversal path, the commitment is premature and the plan
must be revised before implementation begins.

## Development environment

Windows-only lifecycle. **PowerShell 7 (`pwsh`) is the only approved scripting
language.** Do not use bash, batch (`.bat` / `.cmd`), Python, Node, or legacy
`powershell.exe`. When a shell tool is needed from automation, invoke a script in
`.tools/` directly:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/<Script>.ps1
```

## Build & test

`.tools/` holds the canonical entry points. Prefer them over `dotnet build` /
`dotnet test` except for narrowly targeted runs.

```powershell
# Standard incremental build (skip restore — packages unchanged)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore

# Full Debug build with restore
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1

# Build + run full test suite (excludes end-to-end UI tests by default)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests

# Cold recompile (forces --no-incremental; does NOT clean bin/obj or restore)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -NoIncremental -RunTests

# Build + markdown size / freshness gate (opt-in; CI does not run it)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -CheckMarkdown

# Release build mirroring CI
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -Configuration Release -RunTests

# Tests only against an existing build
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -NoBuild

# Include end-to-end UI tests (slow; off by default and intentionally not in CI)
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -IncludeEndToEnd

# Single test
dotnet run --project tests/<Project> -- --treenode-filter "/*/*/<Class>/<Method>"
```

`TreatWarningsAsErrors=true` is set repo-wide via `Directory.Build.props`. Every
analyzer diagnostic surfaces as a build error. A clean build is the gate for every
commit. `.tools/Invoke-Build.ps1` also runs `Test-ResourceKeys.ps1` to validate the
translation tables — failures here block the build.

## Workflow tools

Additional `.tools/` scripts for everyday coding-agent work:

| Script | What it does |
|---|---|
| `Get-RepoStatus.ps1` | Branch + base ref + categorised file diff + suggested validation commands. `-Json` for piping. |
| `Get-PrCommentQueue.ps1` | Persistent per-PR review-comment queue under `~/.copilot/pr-queues/`. Actions: `Status`, `Next`, `Pop`, `Done`, `Refresh`. Filter by severity. Used by the `feedback-handler` skill. |
| `Get-WorkItem.ps1` | Extracts one backlog block (`E{NN}-F{NN}-UC{NN}-T{NN}`) from `docs/BACKLOG.md` without loading the whole file. Used by the `work-item` skill. |
| `Format-Csproj.ps1` | Normalises `.csproj` XML formatting. `-CheckOnly` for lint. |
| `Invoke-MarkdownGate.ps1` | Per-category size limits + freshness window for agent-loaded docs. Opt-in via `Invoke-Build.ps1 -CheckMarkdown` or run directly. |
| `Invoke-Cleanup.ps1` | JetBrains code cleanup. `-Path`, `-ChangedSince <ref>`, `-CheckOnly`. Run only when the user asks. |

Shared output helpers live in `.tools/PowerShell/Modules/Output.psm1` — every
new script in `.tools/` should import that module rather than redefining
`Write-Step` / `Write-Success` / `Write-Failure` locally.

## Git commits and PR merges

- **Commit your work and open a pull request once the build and tests are
  green.** Never commit or push directly to `main` — always work on a
  feature branch (`git switch -c <topic>` from the current `main`).
- Use a conventional-commit subject (`feat(traffic): …`, `fix(rules): …`,
  `chore(framework): …`, `docs(architecture): …`) and keep the subject ≤ 72
  characters.
- Open the PR with `gh pr create`, link the issue with a `Fixes #<n>` /
  `Closes #<n>` line in the body when one exists, and summarise the scope,
  validation, and any out-of-scope follow-ups.
- **Merge the PR yourself once it is green** (checks pass, review
  resolved, branch current with `main`): `gh pr merge <N> --squash`
  (`--delete-branch` only when no merge queue). If blocked, fix it
  first; only hand back unmerged when something needs human input.

## Project journal

`JOURNAL.md` (at the repo root) is the append-only epistemic memory shared
across agent sessions. It is governed by `.github/journal-protocol.md` — read
that protocol once at session start and append a single area-tagged entry
before ending any session that did real work. Filter reads by tag; never load
the journal end-to-end.

## Project context

Proxyfan is built as a **modular monolith** with a strict three-layer dependency
direction:

```
Clients (Client, Client.Desktop, Cli)
   │
   └─► Presentation (Shell, Traffic, Tools, …)
          │
          └─► Domain.* kernel + bounded contexts
                 ▲
                 │
                 └─── Framework.* (Networking, Serialization, Platform, …)
```

- **Domain.\*** modules contain business logic and depend only on `Domain` (the
  kernel) and — where stated in the module description — on a sibling domain
  module (e.g. `Domain.Session` → `Domain.Traffic`).
- **Framework.\*** modules implement adapters for the domain abstractions and may
  depend on the matching `Domain.*` module plus `Framework` and `Domain` (kernel).
- **Presentation\*** modules consume `Domain.*` abstractions only. They never
  reference `Framework.*` implementations.
- **Domain → Framework** and **Domain → Presentation** are forbidden.
- **Presentation → Framework** (concrete) is forbidden; presentation goes through
  the DI container.

Architectural tests in `tests/*.Tests` enforce the rules via `ArchUnitNET`.

## Path-scoped instructions

Detailed rules live in `.github/instructions/*.instructions.md`. They auto-load
when the changed file matches the file's `applyTo:` frontmatter:

| File | Scope |
|---|---|
| `architecture.instructions.md` | `src/**/*.cs`, `tests/**/*.cs` |
| `csharp-rules.instructions.md` | `**/*.cs` |
| `testing.instructions.md` | `tests/**/*.cs` |
| `mvvm.instructions.md` | Presentation + Client `*.cs` |
| `avalonia.instructions.md` | `**/*.axaml`, `**/*.axaml.cs` |
| `networking.instructions.md` | `src/Framework.Networking/**/*.cs`, `src/Domain.Proxy/**/*.cs` |
| `scripting-sandbox.instructions.md` | `src/Domain.Scripting/**/*.cs` |
| `localization.instructions.md` | `**/*.resx`, `**/Resources/**/*.cs` |
| `review-gates.instructions.md` | `src/**`, `tests/**`, `.tools/**` |
| `journal-protocol.md` | `JOURNAL.md` (auto-loaded when editing the journal) |

## Skills (`.github/skills/`)

Each skill is invoked by name (e.g. `/architect`, `/proxy-pipeline`). The
`agentic-workflow` orchestrator fans out to every specialist in parallel and
produces a single prioritised plan; see `.github/skills/agentic-workflow/SKILL.md`
for the catalogue.

Engineering-quality specialists: `architect`, `domain-driven-design`, `backend-swe`,
`code-health`, `code-duplication`, `performance`, `asynchrony`, `quality-assurance`,
`regression`, `bug-bounty`, `security-hardening`, `serialization`.

Proxyfan-domain specialists: `proxy-pipeline`, `transport-security`, `rule-engine`,
`traffic-store`, `scripting-sandbox`, `session-format`, `configuration`, `avalonia`,
`protocol-parsers`, `cli-automation`.

Workflow / meta: `devil-advocate`, `doctor`, `product-manager`, `user-experience`,
`triage`, `feedback-handler` (PR review-comment loop), `work-item` (backlog-item
planner).

## When in doubt

1. `docs/ARCHITECTURE.md` defines the layer and module map.
2. `docs/DESIGN.md` defines the behavioural contract for every feature.
3. `docs/BACKLOG.md` enumerates planned work items (referenced as
   `E<epic>-F<feature>-T<task>` in commit messages).
4. The path-scoped instructions and skill files are the authoritative source for
   coding conventions and review process.

If a request appears to conflict with these instructions, surface the conflict and
ask for clarification before proceeding — never silently override.
