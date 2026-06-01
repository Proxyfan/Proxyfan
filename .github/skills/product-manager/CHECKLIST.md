# Product-manager checklist

Detailed reference for the `product-manager` skill.

## Source-of-truth documents

- `docs/DESIGN.md` — product design, features, user workflows,
  behavioural specifications. The single source of truth for "what
  the user expects".
- `docs/BACKLOG.md` — work items organised by milestone, tagged with
  IDs of the shape `E<epic>-F<feature>-T<task>`. Commit messages cite
  these IDs.
- `docs/ARCHITECTURE.md` — the technical decomposition. Cross-reference
  to confirm a proposed feature has a sensible architectural home.
- `README.md` — the externally-facing description. A change that
  alters the README's promised feature set is automatically high-impact.

## Analysis

1. **Backlog alignment.** Map the change to a backlog item where
   possible. Flag work that does not appear in the backlog and is more
   than a small bug fix — it likely needs a new item with a milestone
   anchor.

2. **Requirement coverage.** For each user-visible behaviour the change
   affects, locate the matching section of `docs/DESIGN.md`. Flag:
   - Behaviours implemented without a spec line.
   - Spec lines the change contradicts.
   - Spec lines the change leaves only partially covered.

3. **User flows.** Walk the end-to-end flow the user actually performs
   — not the API call sequence. Examples:
   - "User adds a Map Local rule for `https://api.example.com/users` and
     selects `users.json` from disk" — the flow includes the dialog,
     the file picker validation, the rule list update, the live
     application on the next matching request, and the persistence to
     `config.yaml`.
   - "User opens a captured HTTPS session, decrypts a flow, exports the
     selection as HAR" — the flow includes the SSL proxying list
     gating, the inspector panel, the export dialog, the filename
     suggestion, and the resulting file on disk.
   Flag steps the change skips, breaks, or leaves under-specified.

4. **Edge cases.** For every user flow touched, enumerate the boundary
   conditions:
   - Empty inputs.
   - Capacity-limit inputs (10,000+ flows).
   - Unicode / RTL / surrogate-pair text in fields.
   - Concurrent edits from two tool windows.
   - Hot-reload while a captured connection is mid-flight.
   - Restart while a HAR export is in progress.
   - Disk-full / read-only-volume scenarios.

5. **Discoverability.** A new feature is discoverable if a user without
   reading docs can find it in the UI within three interactions. Flag:
   - Features added only to the config file with no UI surface.
   - Features added to a tool window with no menu entry.
   - Keyboard-only shortcuts with no on-screen affordance.

6. **Documentation parity.** A change to behaviour requires a matching
   doc update:
   - `docs/DESIGN.md` for the user-facing behaviour.
   - `docs/ARCHITECTURE.md` for any architectural ripple.
   - `docs/BACKLOG.md` to mark the item complete (or to file a follow-up).
   - The `Resources/Strings.resx` family for new user-visible strings.

7. **Accessibility implications.** WCAG 2.1 AA is the floor. A new UI
   surface needs:
   - Keyboard navigation.
   - Screen-reader names and help text.
   - Sufficient colour contrast (sourced from the theme).
   - No reliance on colour alone to convey state.

8. **Telemetry / privacy.** Proxyfan ships no telemetry by default.
   A new feature must not introduce an external network call beyond
   user-initiated traffic and the update checker. Flag any opt-out
   telemetry pattern.

9. **Milestone risk.** Map the change to the current milestone
   (`docs/BACKLOG.md`). Flag:
   - Work that pulls items from a future milestone forward without an
     explicit milestone update.
   - Work that defers a current-milestone item without an explicit
     decision.

10. **Failure surface.** When the feature fails, what does the user see?
    Confirm:
    - Errors surface via the existing toast/status-bar/dialog plumbing,
      not via a modal dialog (per the dialog policy).
    - Error messages are user-meaningful — not a stack trace, not a
      `DomainError.Code`.
    - The user can retry, cancel, or escalate to the logs without
      losing their place in the app.
