# Feedback handler — process

Run these steps in order. Do not improvise.

## Step 0 — Resolve the PR

Get the PR identifier from the user. Accept either a number (when the cwd is
inside the Proxyfan repo) or a full URL. Confirm the resolved owner / repo /
number back to the user before doing anything else.

## Step 1 — Refresh the queue

Always start with a refresh so the queue reflects the current state on
GitHub, including any comments the user added since the previous session:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-PrCommentQueue.ps1 `
    -Pr <number-or-url> -Action Refresh
```

Read the printed summary: how many pending / in_progress / resolved.

If the queue is empty, stop and tell the user the PR has no comments to
address.

## Step 2 — Triage the queue

Inspect the highest-severity tier first:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-PrCommentQueue.ps1 `
    -Pr <pr> -Action Status -Severity MAX,HIGH
```

If any MAX / HIGH comments are pending, work them first. Only descend to
MEDIUM / LOW after the higher tiers are clear or the user explicitly says
otherwise.

## Step 3 — Pop one item and start work

Take **one** comment at a time. Popping it marks the comment `in_progress`,
which protects against a second agent session picking up the same comment.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-PrCommentQueue.ps1 `
    -Pr <pr> -Action Pop -Count 1 -Severity <tier>
```

The command emits a JSON document for the popped comment. Extract:

- `id` — keep this; you will need it to mark the comment done.
- `body` — the actual review comment.
- `file` and `line` — for review comments only; absent for issue comments.
- `url` — link back to the GitHub thread.

## Step 4 — Decide whether to apply

Read the comment carefully against the current source. There are exactly
three outcomes:

1. **Apply.** The comment identifies a real issue and the fix is in scope.
   Implement the change. Run the validation commands that match the change
   set (see *Validation* below).
2. **Disagree and surface.** The comment is incorrect, out of scope, or
   would conflict with a higher-priority decision already on file. Stop and
   tell the user what the comment says and why you disagree. Wait for
   explicit acknowledgement before marking the comment resolved.
3. **Already addressed.** The comment was made before a later commit that
   already fixes the issue. Cite the resolving commit and mark resolved.

You may not skip a comment by quietly leaving it `in_progress`. Always
either apply, surface, or close it with a citation.

## Step 5 — Validate the change

Match the change to the appropriate validation command. Use
`Get-RepoStatus.ps1` to confirm the actual file scope and suggested commands:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-RepoStatus.ps1
```

Then run what it suggests. Typical paths:

- C# / csproj / MSBuild change:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1 -SkipRestore -RunTests`
- .resx change: `pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Test-ResourceKeys.ps1`
- Doc-only change: `pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-MarkdownGate.ps1`

Build / test failures count as new findings. Address them before moving on
to the next comment; never leave the working tree broken between popped
items.

## Step 6 — Mark the comment resolved

After validation succeeds:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Get-PrCommentQueue.ps1 `
    -Pr <pr> -Action Done -Id <comment-id>
```

The script prints the updated status table; verify the comment moved to
`resolved`.

## Step 7 — Loop or exit

If `pending > 0` and you have time / context budget left, go back to
**Step 3** with the next-highest-severity item.

If `pending = 0` or the user wants to stop, finish the loop:

1. Print the final snapshot:
   `Get-PrCommentQueue.ps1 -Action Status`.
2. Run the build + test gate one last time
   (`Invoke-Build.ps1 -SkipRestore -RunTests`).
3. Commit with a conventional-commit subject and push:
   `git commit` then `git push`.
4. Resolve the GitHub review threads (`resolveReviewThread` GraphQL
   mutation, or accept the auto-resolve when the reviewer set it).
5. Merge the PR (`gh pr merge <N> --squash`, add `--delete-branch`
   only when merge queues are off). If the merge is blocked
   (failing checks, stale branch, unresolved disagreement), fix
   the blocker first; only hand the PR back unmerged when something
   concrete needs human input.
6. Append a journal entry tagged at minimum with the affected systems
   (e.g. `[traffic,inspector,tests]`) plus `[feedback]`, capturing the
   three bullets per `.github/journal-protocol.md`.
