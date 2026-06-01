# Code-duplication — detection process

The skill runs in two modes. Choose based on the invocation.

## Mode A — Whole-codebase analysis

1. **Sweep the solution.** Read the source tree under `src/`, focusing on:
   - `Framework.Networking` — protocol parsers and forwarders have many
     near-twin classes; check for genuine vs accidental duplication.
   - `Domain.Rules/Rules/` — rule families (`AllowListRule`, `BlockListRule`,
     `MapLocalRule`, `MapRemoteRule`, `BreakpointPause`, `NoCachingRule`)
     for shared matching / rewriting logic.
   - `Domain.Traffic/` — the four flow stores (`TrafficStore`,
     `WebSocketStore`, `ServerSentEventsStore`, `RemoteProcedureCallStore`)
     for shared ring-buffer / observation patterns.
   - `Framework.Serialization` — HAR / JSON / YAML / Protobuf /
     MessagePack writers for shared serialisation patterns.
   - `Presentation.*` ViewModels — projection logic from a domain type into
     an immutable `ViewModelInfo`-style record.
2. **Triage candidate clusters.** Order by:
   1. Total duplicated lines.
   2. Cross-project spread (cross-project duplication is structurally
      worse than intra-project).
   3. Whether divergence would silently break a user-facing feature.

## Mode B — PR / branch / working-tree review

1. **Identify the changeset.** `gh pr diff <PR>` or `git diff <base>..HEAD`
   captures the changed files and added/modified line ranges per file, plus
   the new types, methods, processors, rules, or stores introduced.
2. **Filter to the diff.** Keep only clusters where at least one instance:
   - Lives in a file that the diff touches.
   - Has a line range overlapping an added or modified line range, **or**
     - The diff introduces a new method/type whose behaviour duplicates
       something already in the repo.
3. Pre-existing duplication in files merely touched is out of scope in
   review mode — surface it through a Mode-A run instead.

## Shared steps

### Find the root structural issue

For every cluster you keep, answer all four questions from `PERSONA.md`. If
you cannot answer them with Proxyfan-specific evidence, suppress.

### Look for semantic duplication

Pure syntactic clusters are easy; the harder finds are semantic:

- The same validation rule expressed in two places (e.g. a port-range check
  in `ProxyOptionsValidator` and in `ReverseProxyOptions`).
- The same byte / header parse coded twice (e.g. `HypertextTransferProtocolHeaderParser`
  vs an inline parser in a sibling handler).
- A coordinate / size calculation duplicated in `Domain.Throttling` and a
  consumer in `Framework.Networking`.
- A parallel implementation of a pipeline step — a new `IRequestPhaseRule`
  added beside an existing one when the existing one would have extended
  cleanly.
- A new `*Helper` named like an existing helper that overlaps in purpose.

### Watch for agent-induced fragmentation

The codebase receives narrow edits from various contributors. Aggressively
flag:

- New `if` / `switch` branches added to an existing forwarder or
  orchestrator to handle "just this case".
- New `*Dependencies` record that mostly mirrors an existing one with one
  field changed.
- New `Mutable*` configuration type that should have been a new property on
  an existing mutable type.
- Configuration duplicated across DI registration, options binding, and
  hard-coded constants.

## Forbidden silencers in proposed fixes

When recommending a refactor, never suggest `#pragma warning disable`,
`[SuppressMessage]`, `<NoWarn>` additions, or `params` arguments. The
analyzer rules are absolute.
