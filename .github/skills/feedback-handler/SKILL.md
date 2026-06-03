---
name: feedback-handler
description: Resolves pull-request review comments on a Proxyfan PR systematically — works through the queue, addresses one item at a time, and marks it done. Use when the user asks to "address PR feedback", "go through review comments", or names a specific PR number and asks for changes.
---

# Feedback handler

Drives the loop that turns external GitHub review comments on a Proxyfan PR
into a sequence of focused, verifiable code changes — across as many agent
sessions as the work takes — without losing track of what is done.

## When to invoke

The user has asked you to address feedback on a specific PR. They will name
the PR by number, URL, or by being currently checked out on the topic branch.
If the user has not named a PR, do not invoke this skill; ask them which PR
first.

## What you OWN end-to-end

1. **Hydrate the queue** for the named PR from GitHub.
2. **Walk the queue** in priority order (MAX → HIGH → MEDIUM → LOW, oldest
   first within each tier).
3. **For each item**: read the comment, decide whether to apply, implement the
   change, validate locally, mark the comment resolved in the queue.
4. **Report progress** between items so the user can interrupt the loop.

## What you do NOT own

- Deciding whether to ignore a comment — see the *Disagreeing with a comment*
  section; you must surface disagreement to the user before skipping.

You DO own the rest of the lifecycle: implementing the fix, committing,
pushing, resolving the GitHub review threads, and merging the PR once the
queue is clean and the checks are green. Hand the PR back unmerged only
when something concrete blocks the merge (failing checks, conflicts you
can't resolve, an unresolved disagreement).

## Tooling contract

This skill is built on `.tools/Get-PrCommentQueue.ps1`. The queue is persisted
under `~/.copilot/pr-queues/pr-<owner>-<repo>-<number>.json` and is
authoritative across sessions for the **status** of each comment
(`pending` / `in_progress` / `resolved`). Comment bodies always come from
GitHub (refresh before starting work).

The process is documented in `PROCESS.md`. Read it once at the start of the
session and follow it step by step.

## Disagreeing with a comment

You may not silently skip a review comment. If you disagree:

1. Surface the comment to the user with your reasoning in two or three lines.
2. Wait for explicit acknowledgement before marking it resolved.
3. If acknowledged, mark it `resolved` with a note in the journal entry at
   end of session explaining the reasoning so a future agent does not redo
   the same analysis.

## Exit

When the queue has zero `pending` items, summarise:

- How many items were addressed and how many were marked resolved without
  changes.
- Files touched (grouped by module).
- Suggested validation commands (build, test, markdown gate as relevant).
- The user's next step — typically `git add -p` followed by `git commit`,
  then `git push`.
