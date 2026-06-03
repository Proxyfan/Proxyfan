---
name: work-item
description: Loads a single backlog work item (E{NN}-F{NN}-UC{NN}-T{NN}) from docs/BACKLOG.md and turns it into an executable plan with the right cross-references and validation commands. Use when the user names a backlog id ("implement E04-F02-UC01-T01", "let's plan E10-F03").
---

# Work-item planner

`docs/BACKLOG.md` is the source of truth for Proxyfan's planned work. Each
task is fully self-contained — requirement, scope, technical constraints,
implementation guidance, test requirements, definition of done, risks — but
the file is 1800+ lines. Loading it end-to-end is wasteful and pulls
unrelated context into the agent window.

This skill loads only the requested block, cross-references it against the
architecture / design documents, and produces a tight, validated plan that
respects Proxyfan's gates (see `.github/instructions/review-gates.instructions.md`).

## When to invoke

The user has named a backlog id or asked you to "plan" / "scope" / "start"
a backlog item. The id may be partial:

- `E10` — entire epic. Plan the **next** unblocked feature unless told
  otherwise.
- `E10-F01` — feature. Plan it as a single shipping unit.
- `E10-F01-UC01` — use case. Plan it (typically one PR).
- `E10-F01-UC01-T01` — single task. Plan it directly.

If the user has not named a backlog id, do NOT invoke this skill. Ask them
to point at one.

## What you OWN

1. Loading only the relevant block from `docs/BACKLOG.md` via
   `.tools/Get-WorkItem.ps1` (or fetching the GitHub issue body when the
   user names an issue instead of a backlog id).
2. Producing a plan that satisfies the gates in
   `.github/instructions/review-gates.instructions.md`:
   - scope (in / out), effort sketch, design, reversibility, privacy
     posture;
   - an ADR paragraph if the change introduces a new module, extensibility
     interface, package, or domain → domain dependency.
3. Dispatching the right specialist skills against the plan **before**
   coding starts (see `.github/skills/agentic-workflow/CATALOG.md`).
4. Confirming the plan with the user before any file write.
5. **Implementing the plan**, running the full build + test gate, and
   landing the change on a feature branch.
6. **Opening a pull request** for every accepted plan — the skill is not
   complete until `gh pr create` has returned a PR URL. The PR body links
   the work item (`Fixes #<n>` for a GitHub issue, or the backlog id for a
   backlog-only item) and summarises scope, validation, and any
   out-of-scope follow-ups surfaced during planning.

## What you do NOT own

- Editing `docs/BACKLOG.md` to mark the item complete. That is a separate
  bookkeeping step the user owns.

You DO own merging the PR once the checks are green and any review
feedback is addressed. Hand the PR back unmerged only when something
concrete blocks it (failing checks, conflicts, unresolved review threads
that need human input).

## Tooling contract

This skill is built on `.tools/Get-WorkItem.ps1`. The full process is in
`PROCESS.md`. Read it once at the start of the session and follow it step
by step.
