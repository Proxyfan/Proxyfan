# Doctor — forbidden-fix catalogue

Applied in **Phase 4** of `PROCESS.md`, before any plan is proposed. A
single match in any category blocks the proposal. Do not negotiate,
partially apply, or "compromise". Propose a root-cause alternative; if
the alternative needs specialist confirmation, return to Phase 3.

| Category | Rationale |
|---|---|
| **`#pragma warning disable` / `#pragma warning restore`** | Silences an analyzer ID. Forbidden across `src/` and `tests/` per `AGENTS.md`. Fix the violation. |
| **`[SuppressMessage]` attributes** | Same as above — silencer. |
| **Editing `<NoWarn>` to clear an active failure** | A deliberate exception is a separate, reviewed PR — never bundled with the failing change. |
| **Editing `.editorconfig` severity** | Severity changes belong in their own deliberate PR. |
| **Conditional compilation skips** | Wrapping the failing code in `#if !DEBUG` / `#if RELEASE` to exclude it from the failing configuration. |
| **Deleting source files** | Removing the file that contains the failure to clear it. Deletion is only legitimate when removal itself is the intent. |
| **Deleting tests** | Removing a failing test (or its containing class) to clear the failure. Both production code and test expectation have legitimate fixes. |
| **Commenting out tests** | Surrounding a failing test (or assertion) with `/* ... */` or `//`. Treated identically to deletion. |
| **`[Skip]` / `[Skip(reason)]`** | Skipping a failing test to clear the failure. |
| **`[Explicit]`** | Marking a test `[Explicit]` so the default run no longer executes it. |
| **Assertion loosening** | Weakening an assertion to pass a failing test without addressing root cause (`IsEqualTo` → `IsNotNull`, removing an assertion, swapping strict equality for "contains"). |
| **Retry attributes to mask flakes** | `[Retry(N)]`, polling loops, or sleep-and-retry to mask a race / state-leak. Flakes have a real root cause; fix it there. |
| **Coverage threshold lowering** | Reducing the configured coverage gate to clear a failing build step. |
| **Cosmetic resource-keys "fixes"** | Adding whitespace or comment-only changes to a `.resx` file to pass `Test-ResourceKeys.ps1` when the real issue is a missing or extra key. The script wants real key parity. |
| **Hand-editing `Strings.Designer.cs`** | The designer file is generated; never hand-edit. Fix the upstream `.resx` and rebuild. |
| **`#if` around an analyzer error** | Equivalent to suppression. Fix the root cause. |
| **Committing directly to `main`** | The skill never commits to a base branch, even in main-rescue. Fixes ship through a topic branch and a PR. |
| **Squashing iteration commits before the gate** | One commit per invocation, after the final build + tests gate is green. Pre-emptive amend / interactive rebase before the gate is forbidden. |

When a forbidden fix is matched, log the match to the session user with
the matched category and the trigger (file, ID, attribute, or diff
snippet), then propose an alternative root-cause fix. No fix in the
forbidden list ever ships through this skill.
