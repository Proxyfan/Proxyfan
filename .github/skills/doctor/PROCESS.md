# Doctor — per-phase process

`SKILL.md` keeps the phase index and operating principles. `POLICY.md`
holds the forbidden-fix catalogue. This file holds per-phase detail.

## Inputs and overrides

Free-form prompt + optional named overrides:

- `base: <ref>` — branch to cut from. Default `main`. The local ref is
  authoritative (no `git fetch` is implicit).
- `branch: <name>` — full topic-branch name. Skips the data-driven
  naming in Phase 6. Must include the `fix/` or `chore/` prefix.

Reject the invocation if the working tree is dirty
(`git status --porcelain` non-empty). The skill never stashes or commits
user changes.

## Invocation scenarios

| Scenario | Detection | Framing | Branch prefix |
|---|---|---|---|
| **Topic-branch rescue** | `HEAD` on a non-base branch. | Standard proposal. | `fix/` or `chore/` (data-driven). |
| **Main-rescue** | `HEAD` on the base; build failing. | Proposal opens with `MAIN-RESCUE` + one-line root cause. PR body leads "main was red because …". | `fix/restore-main-<short-cause>`. |

Both scenarios cut a fresh topic branch from the base. Never commit
directly to `main`.

## Phase 0 — Resolve inputs

Verify preconditions; refuse to proceed if any fail.

- `git status --porcelain` empty.
- Base ref exists locally.
- `branch:` if provided starts with `fix/` or `chore/`.

Detect topic-branch vs main-rescue from
`git rev-parse --abbrev-ref HEAD`.

## Phase 1 — Initial build + tests

Run:

```pwsh
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests
```

Stream and capture full output. If `dotnet restore` fails up front
(NU####, NETSDK####), re-run without `-SkipRestore`.

If green: exit with "nothing to do" — no branch, no commit.

Otherwise proceed to Phase 2 with the captured output.

## Phase 2 — Triage

Parse the captured output into a structured failure list; bucket by
category and by file + root cause.

### Build-output parser

Recognise these shapes:

- **Compile errors**: `<path>(<line>,<col>): error <CSnnnn>: <message>`.
- **Analyzer violations**: same shape with `ATX…NNN`, `S…NN`, `CA…NNN`,
  `IDE…NNN`, `CS…NNN` IDs (warnings-as-errors surface as `error`).
- **Restore failures**: `error : NUnnnn:` / `error : NETSDKnnnn:`
  before the build phase.
- **Resource-keys failures**: lines under `[FAIL]` from
  `Test-ResourceKeys.ps1`, naming the key, the locale file, and the
  mismatch shape (extra, missing, placeholder-count).

Deduplicate by `(category, file, ID-or-message)`. Group adjacent
occurrences of the same root cause.

### Test-output parser

Recognise TUnit failure shapes from `Run-Tests.ps1`:

- Test summary line with failed count > 0.
- Per-failure: fully-qualified test name (`Namespace.Class.Method(args)`),
  assertion message, stack trace.
- Setup / fixture failures bucketed separately — the fix shape differs.

Record per failure: project, class, method, top stack frame inside
`src/`.

### Mechanical vs manual split

Cross-check every analyzer-category failure against the cleanup tool
(`.tools/Invoke-Cleanup.ps1`). Mechanical-eligible failures form
Batch A; everything else is Batch B (manual, one entry per item in the
proposal).

## Phase 3 — Specialist sweep

Dispatch `/agentic-workflow` exactly once with the structured failure
list:

- Failure list (categories, files, IDs, root causes).
- Current scenario (topic-branch vs main-rescue).
- Recent git log on the base (last 10 commits) for main-rescue.
- List of edited files for topic-branch.

Specialists return per-finding guidance: product-side vs test-side,
deeper architectural symptom, sibling tests to check. Use this to refine
the proposal — never to expand scope.

## Phase 4 — Forbidden-fix filter

Walk every Phase 3 candidate against `POLICY.md`. A single hit blocks
the proposal: report the matched category, propose an alternative
root-cause fix, return to Phase 3 if specialist confirmation is needed.

## Phase 5 — Propose

Present the proposal in chat. Use the `ask_user` tool (or an equivalent
gate) to make approval explicit. Shape:

```
DOCTOR PROPOSAL — <topic-branch | MAIN-RESCUE>
Base: <ref>   Branch: <prefix>/<slug>

Summary: <1-3 sentences on what's broken and why>

Failures (by category):
- <category>: <count> — top examples cited with file:line

Plan:
- Batch A (cleanup): <file list>
- Batch B (manual): <file>: <fix> — <root cause + specialist guidance>

Risk / open questions: <what the user must weigh in on>
```

Wait for explicit approval before continuing.

## Phase 6 — Cut branch

After approval, decide the topic-branch name (unless `branch:` was
provided):

- **Prefix decision.** Count failures by category. If
  `compile + test + runtime > analyzer + resource-keys + restore`, use
  `fix/`; otherwise `chore/`. Main-rescue is always `fix/`.
- **Slug heuristics.** From the dominant category + a short cause.
  Examples: `fix/restore-main-cs0246-missing-using`,
  `chore/format-rule-pipeline-batch`,
  `fix/traffic-store-eviction-regression`.

```pwsh
git switch -c <prefix>/<slug> <base>
```

Abort on precondition failure (dirty tree, missing base, name
collision).

## Phase 7 — Implement

Order:

1. **Batch A — mechanical.** Run `.tools/Invoke-Cleanup.ps1` only when
   the user explicitly authorised it as part of the plan (the
   `.tools` README marks it on-demand-only). Otherwise hand-apply the
   matching mechanical fix.
2. **Batch B — manual.** Per the approved proposal:
   - **Compile errors.** Address the underlying type / namespace /
     signature mismatch.
   - **Non-mechanical analyzer violations.** Rewrite to comply. Never
     silence via `#pragma`, `SuppressMessage`, or `<NoWarn>`.
   - **Resource keys.** Add the missing key to every locale file, with
     a meaningful translation. Cosmetic touches are forbidden.
   - **Restore failures.** Update the package reference in
     `Directory.Packages.props`; re-run with restore enabled.
   - **Test failures.** See policy below.

### Test-failure fix policy

Decide test-side vs production-side using specialist guidance:

- **Production-side.** The test expectation is correct; fix the
  production code. Cite the specialist finding in the commit.
- **Test-side.** The test expectation is wrong (stale, copy-pasted,
  obsolete contract). Update the test with a cited justification
  linking to the production behaviour that proves the new expectation.

Forbidden test fixes per `POLICY.md`: `[Skip]`, `[Explicit]`, deleting
the test, weakening or removing assertions, `[Retry(N)]` / polling
loops to mask flakes.

## Phase 8 — Re-verify loop

Re-run `Invoke-Build.ps1 -RunTests` until clean. On a recurring failure
(same ID + file + test name after a targeted fix), abort with a stuck-
failure report rather than spinning.

When green, run one final cold build:

```pwsh
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -RunTests
```

(The first iteration may have used `-SkipRestore` for speed; the final
gate restores to mirror CI.)

## Phase 9 — Commit

One focused commit per invocation. Conventional subject:

```
<type>(<scope>): <imperative summary>

Addresses: <one-line gist>
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Omit the trailer only when the user explicitly opted out.

## Phase 10 — Push + PR

```pwsh
git push -u origin <branch>
gh pr create --base <base> --head <branch> --title "<subject>" --body "<body>" --fill
```

Main-rescue PRs lead the body with "main was red because …".
