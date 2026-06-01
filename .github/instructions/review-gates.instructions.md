---
applyTo: "src/**,tests/**,.tools/**"
---

# Review gates

These gates run in order. Skipping or reordering them is a policy violation.
They exist to catch the failures that pass every other automated check —
analyzer rules, tests, and ArchUnitNET conformance.

## Gate 1 — Plan review

For any change that is more than a single-file mechanical edit, draft the plan
**before** touching code. The plan covers:

- Scope (in/out, listed modules touched).
- Effort (rough size and complexity).
- Design (architecture sketch, listing affected interfaces).
- Reversibility (six-month rollback path).
- Privacy posture (does anything new touch bodies, headers, certificates, or
  the on-disk shape?).

A change that introduces a new module, new extensibility interface, new
package, or new domain → domain dependency requires a one-paragraph ADR:
**Context → Decision → Alternatives rejected → Reversal path**. If the reversal
path cannot be described, the plan must be revised.

## Gate 2 — Specialist sweep (second pass)

Before implementation, dispatch the `agentic-workflow` orchestrator (or the
relevant individual specialist) over the plan or the working tree:

- `architect` — boundary and layering review.
- `domain-driven-design` — context and aggregate review for new domain code.
- `backend-swe` — contracts, error handling, options binding.
- `code-duplication` — am I about to add a parallel implementation?
- `regression` — what existing behaviour does the change risk drifting?
- Domain specialists for the affected surface (`proxy-pipeline`,
  `rule-engine`, `transport-security`, `traffic-store`, `scripting-sandbox`,
  `session-format`, `protocol-parsers`, etc.).

Specialist findings are treated as **assumptions to challenge**, not as
mandates. Adopt those that prevent a real bug; set aside those that would
significantly complicate the change without commensurate benefit. Document
the rationale for setting a finding aside in the PR body.

## Gate 3 — Stop-and-ask escalations

After the specialist sweep, stop and ask the user before:

- Adding a new project to `Proxyfan.slnx`.
- Adding a new package to `Directory.Packages.props`.
- Changing a public contract on a `Domain.*` abstraction.
- Adding or removing an extensibility interface
  (`IContentDecoder`, `ITrafficInspector`, `IExportFormatter`,
  `IConnectionHandler`, `IUserScript`, …).
- Changing the on-disk shape of HAR export, the YAML configuration schema,
  the certificate store layout, or the log directory structure.
- Deleting tests, lowering the coverage gate, or weakening an assertion.
- Doing anything not explicitly described in the approved plan.

## Gate 4 — Clarifying questions

After the stop-and-ask gate, surface clarifying questions for decisions that
could break things. Only ask questions that **must** be answered by a human —
not operational noise. Examples that warrant asking:

- Two reasonable implementations exist and the choice affects users.
- The plan and the actual code state diverge unexpectedly.
- A specialist finding conflicts with the user's stated intent.

Examples that **do not** warrant asking:

- The naming style of an internal helper.
- The order of two independent refactors.
- Whether to follow an established repository convention.

## Gate 5 — Post-plan discovery

After the plan is approved, every newly discovered fact creates a new task in
the working list. This keeps the agent grounded on the original goal rather
than silently expanding scope, fast-tracking, or losing context during
compaction. New tasks are surfaced to the user before being acted on if they
expand the scope agreed in the plan.

## Gate 6 — Pre-commit cold build

Before any commit ships through automation:

1. `pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Invoke-Build.ps1`
   (or `-RunTests` if test outcomes are relevant to the change).
2. **Zero** new errors. Pre-existing warnings on the base branch are out of
   scope; do not silence them. New warnings are forbidden.
3. `Test-ResourceKeys.ps1` is included in `Invoke-Build.ps1` — a resource
   regression fails the build automatically.

A commit that ships without a passing build is a policy violation regardless
of how trivial the change looks.

## Gate 7 — Privacy posture

Any change that touches `Framework.Networking`, `Domain.Traffic`,
`Domain.Session`, `Framework.Serialization`, or any logging path must be
reviewed for privacy regressions:

- Bodies are not logged.
- Headers are only logged at `Trace` level.
- `Authorization`, `Cookie`, `Set-Cookie` are redacted at every level.
- No telemetry is added without an explicit user opt-in setting **and** a
  documented disable path.
- No external network calls outside user-initiated traffic and the update
  checker.

A privacy regression is a P1 finding — it blocks the change.
