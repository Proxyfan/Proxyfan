# Work-item planner — process

Run these steps in order.

## Step 0 — Confirm the id

Echo the backlog id back to the user and confirm the resolved scope before
spending tokens on it:

> "You named `E04-F02-UC01-T01`; that's task T01 inside UC01 / F02 / E04
> (`<feature title>`). Shall I scope just this task, or include its sibling
> tasks under the same use case?"

For an epic-level id (`E10`) or feature-level id (`E10-F01`), list the
children first so the user can pick:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-WorkItem.ps1 `
    -Id E10 -List
```

Do NOT proceed to Step 1 with an ambiguous id.

## Step 1 — Load the block

Pull only the targeted block:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-WorkItem.ps1 `
    -Id <id>
```

Read every numbered subsection: requirement, context, expected result,
scope, out of scope, technical constraints, implementation guidance, test
requirements, definition of done, risks and edge cases.

Cite the **architecture / design** anchors mentioned in the block
(e.g. `ARCHITECTURE.md#121-proxy-engine`). Do not paste those documents
into context yet — note the anchors and look them up only if needed.

## Step 2 — Confirm prerequisites

For the id to be implementable now, the predecessor tasks must already be
done. Use the BACKLOG cross-reference pattern (`Implementation guidance:
... reuse the upstream connector (E01-F02-UC01-T03)`) to find dependencies.
For each dependency, verify either:

- the cited type exists in the codebase (`grep` for the type name); or
- the dependency is already complete on the current branch.

If a dependency is missing, surface that fact to the user and offer to
re-scope: either implement the dependency first, or stub it behind an
interface.

## Step 3 — Draft the plan

Produce a plan that addresses the gates in
`.github/instructions/review-gates.instructions.md`:

1. **Scope** — list the modules (e.g. `Domain.Proxy`, `Framework.Networking`)
   that will be touched and the modules that will NOT.
2. **Effort** — rough size (S / M / L) and the number of files.
3. **Design** — name the interfaces and concrete types you will add or
   change. Cite the existing siblings they parallel
   (e.g. "new `IFooBar` mirrors `IBazQux` in `Domain.Traffic`").
4. **Reversibility** — describe the path to revert in six months. If the
   change adds a new package, extensibility interface, public contract, or
   domain → domain dependency, expand this into a one-paragraph ADR
   (**Context → Decision → Alternatives rejected → Reversal path**) per
   the top-level `copilot-instructions.md` quality bar.
5. **Privacy posture** — explicitly note whether the change touches
   request / response bodies, headers, certificates, traffic on disk, or
   any logging path. State whether bodies and credential headers remain
   redacted at every level.
6. **Tests** — list the test projects that will gain new files and the
   shapes of the tests (TUnit method naming `Method_Scenario_Expected`,
   per `.github/instructions/testing.instructions.md`).

## Step 4 — Specialist sweep

Per gate 2 of the review gates, dispatch the relevant specialists against
the **plan** (not the code, which doesn't exist yet). Always include:

- `architect` — boundary / layering.
- `domain-driven-design` — context boundaries (for new domain code).
- `regression` — what behaviour is at risk?
- `code-duplication` — am I about to parallel an existing helper?

Add the domain-specific specialists based on the modules in the scope list:

| Scope contains                        | Add specialists                              |
|---------------------------------------|----------------------------------------------|
| `Domain.Proxy` / `Framework.Networking` | `proxy-pipeline`, `protocol-parsers`, `transport-security` |
| `Domain.Rules`                        | `rule-engine`, `regression`                  |
| `Domain.Scripting`                    | `scripting-sandbox`, `security-hardening`    |
| `Domain.Traffic`                      | `traffic-store`, `performance`               |
| `Domain.Session` / HAR                | `session-format`, `serialization`            |
| `Domain.Certificates`                 | `transport-security`, `security-hardening`   |
| Presentation, ViewModels              | `avalonia`, `user-experience`                |
| `Cli`                                 | `cli-automation`                             |

Treat specialist findings as **assumptions to challenge**. Adopt those
that prevent a real bug; set the rest aside and record the rationale.

## Step 5 — Stop-and-ask

If the plan triggers any of the stop-and-ask conditions in gate 3 of the
review gates, surface the decision to the user before writing the plan
file. Common triggers:

- New project in `Proxyfan.slnx`.
- New package in `Directory.Packages.props`.
- Public contract change on a `Domain.*` abstraction.
- New extensibility interface.
- On-disk shape change (HAR, YAML config, certificate store, log directory).

## Step 6 — Confirm the plan

Present the plan back to the user and wait for acceptance. After
acceptance:

1. Write the plan to `~/.copilot/session-state/<session-id>/plan.md`.
2. Proceed to Step 7. Do not stop at the plan — the skill owns shipping
   the change end-to-end.

## Step 7 — Implement and validate

1. Implement the change exactly as described in the accepted plan. If a
   deviation becomes necessary mid-flight, surface it to the user before
   continuing.
2. Run the full gate from the repo root:

   ```powershell
   pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 `
       -SkipRestore -RunTests
   ```

   Do not proceed to Step 8 until the gate is clean. A flaky single
   project may be retried via `.tools/Run-Tests.ps1 -NoBuild` to confirm.

## Step 8 — Commit, push, and open the pull request

The skill is not complete until a pull request exists. Always work on a
feature branch — never commit directly to `main`.

1. Append the journal entry now (before committing) so it lands in the
   same diff:

   ```powershell
   # Append per .github/journal-protocol.md — single area-tagged entry.
   ```

2. Create a feature branch from the current `main`:

   ```powershell
   git switch -c fix/<short-slug>   # or feat/, chore/, docs/, …
   ```

3. Stage and commit with a conventional-commit subject (≤ 72 chars). The
   `Co-authored-by: Copilot` trailer is mandated at the agent-runtime
   layer and is appended automatically — you do not need to add it
   manually. The commit body should describe the change, cite the
   validation command, and reference the work item (`Fixes #<n>` when a
   GitHub issue exists, or the backlog id `E04-F02-UC01-T01` otherwise).

4. Push the branch and open the PR:

   ```powershell
   git push -u origin HEAD
   gh pr create --fill --base main
   ```

   When the work item is a GitHub issue, make sure the PR body contains a
   `Fixes #<n>` line so the issue auto-closes on merge. Summarise scope,
   validation, and any out-of-scope follow-ups surfaced during planning
   (these typically become new GitHub issues — file them if the user
   confirms, otherwise note them in the PR body for triage).

5. Hand the PR URL back to the user and stop. Do **not** merge — that
   step is owned by the human.
