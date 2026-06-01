# User-experience checklist

Detailed reference for the `user-experience` skill.

## Shell layout

Proxyfan ships a three-panel layout:

| Panel | Owner | Responsibility |
|---|---|---|
| Left — Source List | `Presentation` / `Client/Shell` | Sources / sessions / saved filters. |
| Centre — Traffic Flow List | `Presentation.Traffic` | Captured flows, virtualized. |
| Right — Inspector Panel | `Presentation.Traffic` | Tabs (Headers, Body, Query, Cookies, Auth, Raw, Timing, Summary) for the selected flow. |

A new feature surface must compose with this layout. Adding a fourth
top-level panel is a Phase-3 stop-and-ask change.

## Analysis

1. **Common-flow friction.** Walk the high-frequency flows and count
   keystrokes / clicks:
   - Start the proxy. (Should be < 2 clicks from app launch.)
   - Trust the root CA. (Should be a guided one-button flow with a
     clear elevation prompt.)
   - Select a flow → see the request body. (Should require zero
     clicks beyond selection.)
   - Apply a Map Local rule. (Should be < 5 clicks from the flow's
     context menu.)
   - Add a breakpoint on a host. (Should be < 5 clicks.)
   - Export the current session as HAR. (Should be < 3 clicks.)

   Flag flows that exceed the expected interaction count.

2. **Discoverability.** Each feature surface has at least one of:
   - A menu entry.
   - A toolbar button.
   - A keyboard shortcut, surfaced in the help overlay (F1) and the
     menu's accelerator label.

   Flag a feature only reachable via the config file or a context menu
   buried two levels deep.

3. **Error surfacing.** Errors appear through the existing toast /
   status-bar / overlay plumbing, never a modal dialog. The exception
   is destructive actions (delete root CA, clear all flows) — those
   confirm via a dialog.

   - Error text is user-meaningful (the `Message` field of
     `DomainError`, never the `Code`).
   - A "View details" affordance opens the log file or the diagnostics
     overlay for power users.
   - Retry / cancel / dismiss buttons are explicit and consistent.

4. **Accessibility.** WCAG 2.1 AA, enforced as a floor:
   - Every interactive control has a keyboard equivalent.
   - `AutomationProperties.Name` and `HelpText` on every control read
     by a screen reader.
   - Focus is visible (`StyledProperty<IBrush?> FocusBrush` or theme
     equivalent).
   - Colour is never the sole channel for state — pair colour with an
     icon or text affordance.
   - Theme contrast meets AA. Avoid hard-coding text foregrounds.

5. **Keyboard navigation.**
   - Tab order matches reading order.
   - `Esc` cancels the active modal, overlay, or breakpoint pause.
   - `Ctrl+F` focuses the traffic filter input.
   - `Enter` / `Space` activates the focused button or row action.
   - The keyboard shortcut overlay (`F1` by convention) lists every
     bound shortcut.

6. **Theming.**
   - Light, Dark, and System themes are equivalent — no theme has
     less information than another.
   - Switching at runtime takes effect immediately without a restart.
   - User-defined colour tags (`TrafficFlowColorTag`) read correctly
     against every theme — semi-transparent overlays preferred over
     opaque fills.

7. **Density.** Three density modes (Comfortable / Standard / Compact)
   adjust row heights and padding scales. A new control should fit in
   the Compact mode without truncation.

8. **Consistency.** New controls follow the existing patterns:
   - Buttons: primary (filled, theme accent), secondary (outlined),
     tertiary (text-only).
   - Field labels: above the field, not beside.
   - Validation: inline below the field, with an icon and the resource
     string from the matching `Strings.resx` key.
   - Confirm dialogs: title, body, two buttons (primary action on the
     right; destructive primary uses the danger style).

9. **Internationalisation.** Every user-visible string flows through the
   resource manager. Layouts accommodate longer translations (German /
   French tend to expand 20-30 %); avoid fixed widths.

10. **Performance perception.** UI freezes are the user's primary
    complaint. Flag:
    - Synchronous I/O on the UI thread.
    - Filter inputs that block on each keystroke (debounce in the
      ViewModel, not in the View).
    - Tool windows that block the shell on open.
    - Theme switches that visibly flash before settling.

## Inspector panel

The inspector tabs (`Headers`, `Body`, `Query`, `Cookies`, `Auth`,
`Raw`, `Timing`, `Summary`) are extensible via `ITrafficInspector`.
New tabs:

- Are ordered consistently (`Order` value chosen against neighbours).
- Surface `CanInspect` so they hide when irrelevant.
- Use the existing tab styling.
- Localise the tab title via the resource manager.
