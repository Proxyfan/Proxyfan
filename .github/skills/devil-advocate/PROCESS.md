# Devil-advocate — per-phase process

`SKILL.md` keeps the phase index and operating principles. `POSTURE.md`
holds the suppression doctrine and output schema. This file holds the
exact mechanics.

## Phase 0 — Scope resolution

Resolve the target diff in priority order. Stop at the first match.

1. **Open pull request.** If the invocation references a PR
   (number, URL, or "the open PR for this branch"):
   - Capture the diff: `gh pr diff <N>`.
   - If the base ref is needed: `gh pr view <N> --json baseRefName --jq .baseRefName`.
     **No other fields** of `gh pr view` may be requested.
2. **Current branch vs base.** If no PR is in scope but the current
   branch differs from its base (default `main` unless the invocation
   specifies otherwise):
   - Capture: `git diff <base>..HEAD`.
3. **Working tree.** If neither a PR nor a branch delta exists:
   - Capture: `git diff` (unstaged) and `git diff --staged` (staged).

If all three are empty, exit immediately:

```
## No reviewable diff found

devil-advocate requires a PR, branch delta, or working-tree diff.
Nothing to review.
```

Do not analyse the whole repo. Do not invent a scope. Exit.

## Phase 1 — Cold diff inventory

Read the captured diff in full. Record:

- The list of changed files with added/modified line ranges.
- Every new type, method, public API, persisted shape, dependency,
  boundary, or abstraction introduced.
- Every deletion or relocation of a previously public responsibility.

### Forbidden inputs — hard policy

The following are off-limits for the entire review. Their absence is the
*point*.

| Source | Status |
|---|---|
| `gh pr diff <N>` | ✅ |
| `gh pr diff <N> --name-only` | ✅ |
| `gh pr view <N> --json baseRefName --jq .baseRefName` | ✅ |
| `git diff <base>..HEAD` / `git diff` / `git diff --staged` | ✅ |
| `git log` on the topic branch | ❌ commit messages |
| `gh pr view <N>` without the `baseRefName`-only filter | ❌ title, body, labels |
| `gh pr view <N> --comments` | ❌ review threads |
| Branch name, PR title, PR body | ❌ |
| Linked issues | ❌ |
| Any `plan.md` from a prior session | ❌ |

If the agent has already seen any of these in its context window, treat
that information as **non-evidence**: it cannot be cited in a finding,
and it cannot frame the commitment map.

## Phase 2 — Commitment map (independent)

**Before invoking any specialist**, produce an internal list (do not
include in the final report) of every commitment the diff introduces.
Walk these categories explicitly:

| Category | Enumerate |
|---|---|
| Public API | New public / protected types, methods, properties, events. New required parameters on existing public members. |
| Persisted shape | New / changed HAR shapes, YAML config keys, certificate-store entries, log directory layout. |
| External dependency | New NuGet packages (check `Directory.Packages.props`); new file-system paths; new environment variables. |
| Module boundary | New project references in `.csproj`; types crossing the layer hierarchy. |
| New abstraction | New interfaces, abstract types, providers, factories, builders, `*Dependencies` records — especially with a single concrete caller. |
| Deleted responsibility | Public members removed; logic moved across types or projects. |
| Unexplained intent | Magic constants without a name; conditionals without a named invariant; helpers whose name does not describe their job. |
| Event / message contract | New / changed `IDomainEvent` payloads, callback signatures, command / handler shapes. |
| DI / service lifetime | New service registrations; lifetime changes (Singleton ↔ Scoped ↔ Transient). |

This commitment map is your hostile thesis. Phase 3 will not be allowed
to soften it.

## Phase 3 — Specialist baseline via `/agentic-workflow`

Now and only now, invoke `agentic-workflow` against the captured diff.
Capture its unified report verbatim. Treat each finding it surfaces
— and, critically, each issue it implicitly approves by omission — as
an **assumption to challenge**, not as a fact to adopt. If the consensus
says "the architecture is clean", that is an assumption you should
attack.

### Commitment-class escalation

If the commitment map shows the diff touches a high-risk surface, run
one or two directly relevant specialists *in addition to* the orchestrator
— to interrogate the raw domain risk, not to collect general findings.
Use sparingly.

| Commitment-class surface touched | Escalation specialists |
|---|---|
| New `IDomainEvent` payload / handler change | `event` — no dedicated skill; use `regression` and `architect`. |
| New external dependency / new auth surface | `bug-bounty`, `security-hardening`. |
| New extensibility interface / abstraction | `architect`, `code-duplication`. |
| New persisted shape | `architect`, `regression`, `serialization`. |
| New proxy / TLS / rule path | `proxy-pipeline`, `transport-security`, `rule-engine` as appropriate. |

Do **not** escalate "to be thorough". Escalate only when the commitment
map demands it.

## Phase 4 — Hostile pass

For every commitment in the map, attack it against the closest analogous
good code in Proxyfan. Six questions:

1. **Reversibility.** Could we take this back in a single follow-up PR?
   If not, name the migration that would be needed.
2. **Locality.** Is the new abstraction earning its keep? How many
   callers exist today? How many are forecast in the backlog?
3. **Boundary.** Does any type cross a layer it should not? Is any
   domain type leaking into framework or presentation?
4. **Invariant.** Is there a load-bearing assumption that is not stated
   in code, not tested, and not analyzer-enforced?
5. **Drift.** Does the change quietly diverge from the closest local
   pattern? Why does this new way exist beside the old one?
6. **Intent.** Could the next maintainer reconstruct the intent from the
   code alone?

## Phase 5 — Self-falsification gate

For every finding, ask: *what concrete evidence would make me retract
this finding?* If the honest answer is "none", drop it. If the honest
answer is a specific test result, file content, or pattern check, write
it into the `Retracts if` field.

## Phase 6 — Rejected alternatives

Required for every **Blocking** and **Structural** finding. One-line
shape + one-line trade-off. **No code, no migration steps.** Examples:

- "Extend the existing matcher with a parameterised hook instead of
  introducing a new matcher; trade-off: the hook signature becomes
  load-bearing for two callers."
- "Fold the new processor into `HypertextTransferProtocolForwarder`'s
  dependencies record; trade-off: forwarder's record grows by one field."

## Phase 7 — Severity classification + caps

Classify per `POSTURE.md`. Preferential is capped at 3 — if you have
more, the top three win and the rest are *dropped*, not downgraded.

## Phase 8 — Suppression sweep

Drop findings whose `Retracts if` line is vague, impossible, or untied
to repo evidence. Emit the report per the schema in `POSTURE.md`.

## What you must not become

- A second `agentic-workflow` — if your findings look like dedup of the
  specialist baseline, restart from the commitment map.
- A helpful collaborator — no patches, no code, no migration steps.
- A taste critic — style and naming nits belong to `code-health`.
- A confident guesser — use the Explicit limits block. Often.
