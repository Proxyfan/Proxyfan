# Agentic workflow — per-phase process

The orchestrator runs the same five-phase pipeline whether the context is a
whole-codebase analysis or a diff-scoped review. The scope each specialist
receives is the only thing that changes between modes.

## Phase 0 — Context capture

Pick the context from the invocation:

- **Review mode** (PR, branch delta, or working-tree change):
  - PR: `gh pr diff <PR>` (and `gh pr view <N> --json baseRefName --jq .baseRefName`
    only if the base ref is needed).
  - Branch delta: `git diff <base>..HEAD` (base defaults to `main`).
  - Working tree: `git diff` (unstaged) plus `git diff --staged` (staged).
  - Record the changed-files list and the added/modified line ranges per file
    so specialists can scope their analysis precisely.
- **Analysis mode** (whole-codebase or system sweep): name the projects /
  namespaces / feature surfaces under review. Avoid vague "analyse the whole
  thing" instructions; define the scope explicitly before dispatching.

If both checks come back empty, exit with:

```
## No reviewable scope

agentic-workflow requires either a defined analysis scope or a diff. Nothing
to dispatch.
```

## Phase 1 — Parallel dispatch

Issue one sub-task per specialist from `CATALOG.md` that is relevant to the
captured scope. Sub-tasks run in parallel — they cover different domains and
have no review-time cross-dependencies.

Sub-task template:

```
Sub-task N: Use the /<specialist-skill> skill to <analyze the scope | review the diff>.
Return structured findings: category, severity, location, description, fix.
Scope or diff: <SCOPE-or-DIFF>
```

Wait for every sub-task to return before proceeding to Phase 2.

In **review mode** every sub-task receives the diff (or its scoped slice).
In **analysis mode** every sub-task receives the Phase 0 scope definition.

## Phase 2 — Aggregation

Once all specialists have returned:

1. **Collect** — flatten every finding into one list, noting the originating
   skill.
2. **Deduplicate** — merge findings that describe the same root cause across
   domains. Keep the most actionable description; note both originating
   skills.
3. **Resolve conflicts** — when two specialists recommend contradictory fixes,
   choose the fix that aligns with the analyzer rules
   (`.github/instructions/csharp-rules.instructions.md`), the path-scoped
   instructions, and `docs/ARCHITECTURE.md`. Document the conflict and the
   resolution rationale inline.

## Phase 3 — Prioritisation

Score every deduplicated finding on three axes:

- **Severity** — Critical (crash / data loss / privacy regression / security
  breach) → High (incorrect behaviour / regression) → Medium (performance,
  maintainability) → Low (style, cosmetic).
- **Risk** — likelihood of causing a production incident or regression.
- **Business impact** — effect on the user experience (correct traffic
  capture and rule application > UI correctness > cosmetic).

Compose:

- **P1** — Blocking. All three axes high. Must fix before merge. Privacy
  regressions, correctness defects on the proxy hot path, broken public
  contracts, deleted tests, sandbox escapes.
- **P2** — High. Two axes high. Should fix before merge. Regression risk,
  performance cliff, missing tests on a changed surface.
- **P3** — Medium. One axis high. Nice-to-have.
- **P4** — Observation. None high. Worth recording, never a requested change.

## Phase 4 — Dependency mapping

Identify dependencies between findings: fixing A may be a prerequisite for B.
Group dependent items into **execution blocks**. Items within a block can land
in parallel; blocks land in order.

## Phase 5 — Unified output

Emit one structured report. Two variants, same shape.

### Analysis mode

```
## Proxyfan engineering review — unified action plan

### Executive summary
[2–3 sentences: risk level, P1/P2 counts, cross-domain themes]

### P1 — Critical (block merge)
- **[ID]** [Location] — [Description]
  - Owner: [originating skill]
  - Fix: [Concrete step]
  - Expected outcome: [Observable change after fix]
  - Depends on: [Issue ID or "none"]

### P2 — High (fix before merge)
[Same format]

### P3 — Medium (follow-up)
[Same format]

### P4 — Observation (backlog)
[Same format]

### Execution order
Block 1 (parallel): <IDs>
Block 2 (parallel): <IDs — start after Block 1 lands>
…
```

### Review mode

```
## Proxyfan PR review — consolidated findings

### Summary
[1–2 sentences: scope, overall risk, P1/P2 counts]

### P1 — Blocking
- **[FILE:LINE]** [Description]
  - From: /<skill>
  - Fix: [Concrete change]
  - Depends on: [Issue ID or "none"]

### P2 — Strongly recommended
[Same shape]

### P3 — Nice to have
[Same shape]

### P4 — Observations
[Same shape]
```

## Operating constraints

- **High-signal only.** Empty reports are valid. No theoretical risks without
  a code citation. In review mode, restrict findings to changed files and
  their direct dependents.
- **Consistency across specialists.** A recommendation from one specialist
  must not violate a rule another specialist enforces. The orchestrator is
  the single voice.
- **No regressions suggested.** Never recommend a change that would break a
  passing test, violate an analyzer rule, or remove behaviour without a
  documented gate.
- **Concise output.** Limit each finding to the listed fields. No lengthy
  prose. The unified report must be immediately actionable by a developer.
