# Triage — process

Detailed playbook for the `triage` skill.

## Phase 1 — Reproduce or request reproduction

A bug report without a reproduction is a question. For every bug:

- Does the report name the Proxyfan version (from `Help → About`)?
- Does it name the Windows version?
- Does it list the exact steps?
- Does it include the relevant log excerpt from
  `%LOCALAPPDATA%\Proxyfan\logs\` (with sensitive data redacted)?

If any of these is missing, the next step is a clarifying ask to the
reporter — never proceed with a partial repro.

## Phase 2 — Classify

Pick exactly one type:

- **Bug** — observed behaviour differs from the spec or from the obvious
  expectation. Sub-classify:
  - **Crash** — the app exits or the proxy listener terminates.
  - **Data corruption** — captured flows, HAR exports, or configuration
    files end up in a wrong state.
  - **Privacy regression** — bodies, cookies, or auth headers appear
    where they should not. Always P1.
  - **Correctness** — the proxy alters a request/response when it
    should not, or fails to alter when it should.
  - **Performance** — the app fails to meet one of the budgets in
    `performance/SKILL.md`.
  - **UI** — visual glitch, layout error, broken binding, accessibility
    violation.
- **Feature** — a capability the product does not have. Maps to a new
  backlog item; severity is usually P3 unless tied to a milestone goal.
- **Improvement** — an existing capability that could be better. Maps to
  an addition to an existing backlog item or a new follow-up.
- **Documentation** — `docs/*` is wrong, missing, or out of date.
- **Question** — a usage / configuration question. Maps to a docs
  improvement when the question is likely common.
- **Duplicate** — the issue is already tracked. Point at the existing
  item.

## Phase 3 — Severity

| Severity | Criteria |
|---|---|
| P1 — Blocking | Crash, data corruption, privacy regression, sandbox escape, root-CA exposure. |
| P2 — High | Correctness defect in a common flow; performance regression that breaks a budget; broken UI flow with no workaround. |
| P3 — Medium | Edge-case bug with a workaround; missing feature with a workaround; UI issue that does not block the flow. |
| P4 — Low | Cosmetic, documentation gap, nice-to-have. |

A privacy regression is **always** P1, regardless of how rare the
trigger condition is.

## Phase 4 — Owner module

Map the symptom to the module that owns the failing behaviour. Common
mappings:

| Symptom | Likely owner |
|---|---|
| TLS site fails to load when proxy is running | `Domain.Certificates` / `Framework.Networking` (TLS interceptor) |
| Flow appears in the list with wrong status | `Domain.Traffic` |
| Rule does not apply when expected | `Domain.Rules` |
| Script does not run / errors silently | `Domain.Scripting` |
| Throttling produces incorrect bandwidth | `Domain.Throttling` |
| HAR export missing a field | `Domain.Session` / `Framework.Serialization` |
| Configuration value not honoured | `Domain.Configuration` |
| Reverse-proxy route never activates | `Domain.Proxy` (reverse-proxy registry) |
| UI does not refresh after a rule edit | `Presentation.Tools` (ViewModel) |
| Theme switch leaves stale colours | `Presentation` / `App.axaml` |
| CLI returns wrong exit code | `Cli` |
| Update notification never appears | `Domain.Updates` |
| DNS spoof entry ignored | `Domain.DomainNameSystemSpoofing` |
| Remote-device proxying fails | `Domain.RemoteDevices` |

The owner module determines who the specialist sweep should target if
the issue moves to implementation (`/proxy-pipeline`,
`/transport-security`, `/rule-engine`, `/traffic-store`,
`/scripting-sandbox`, `/session-format`, `/configuration`,
`/avalonia`, `/protocol-parsers`, `/cli-automation`).

## Phase 5 — Backlog placement

Cross-reference `docs/BACKLOG.md`:

- If a matching item exists, attach the report to it as a child task or
  as a comment with a clear repro.
- If a new item is needed:
  - Choose the right epic / feature anchor.
  - Propose the ID (`E<epic>-F<feature>-T<task>`) following the
    existing numbering pattern.
  - Suggest the milestone based on severity and current milestone
    progress.
- If the report is too vague to file ("the app is slow"), the next step
  is a clarifying ask, not a backlog entry.

## Phase 6 — Next step

Be explicit about what happens next:

- **Ask reporter for X.** When a repro is missing or ambiguous.
- **Open PR.** When the fix is small and the path is clear — name the
  expected files and the test that will cover it.
- **Assign to <module>.** When the fix needs deeper investigation by a
  module specialist.
- **Document only.** When the report is a user-error in disguise — the
  next step is a doc update so the next user does not hit the same
  issue.

## What you do not do

You do not implement fixes — `doctor`, `feedback-handler`, and the
specialist skills do that. You do not negotiate severity to placate the
reporter. You do not file duplicates that you could have found by
searching `docs/BACKLOG.md` first.
