---
applyTo: "**/*.axaml,**/*.axaml.cs"
---

# Avalonia UI rules

Avalonia is the rendering layer for every Proxyfan client. AXAML and code-behind
files have hard boundaries with the ViewModel and the rest of the codebase.

## Code-behind (`.axaml.cs`) responsibilities

Code-behind is restricted to **visual concerns** only. The legitimate uses are:

- Wiring up `Behavior<T>` derivatives that need typed access to the control.
- Pointer / drag-and-drop / gesture handling that cannot be expressed in XAML.
- Animation orchestration that benefits from imperative timing.
- Adorner management.
- View-instance lifecycle hooks (`OnAttachedToVisualTree`,
  `OnDetachedFromVisualTree`) for behavior attach/detach.

Code-behind **must not** contain:

- Command wiring (use `Command="{Binding …}"` in AXAML).
- Context-menu construction (use `ContextMenu` in AXAML with `MenuItem`s
  bound to commands).
- Business or domain logic.
- DI lookups, `Application.Current` access, or ViewModel construction.
- Direct mutation of bound data.
- Calls into `Domain.*` or `Framework.*` types.

If a visual concern needs ViewModel coordination, implement an
`Avalonia.Xaml.Interactivity.Behavior<T>` and attach it via
`<i:Interaction.Behaviors>` in AXAML. Behaviors are the seam — code-behind is not.

## Brushes and colours

All `IBrush` and colour values are declared in `App.axaml` as
`Application.Resources`. **No** hard-coded brush or colour appears anywhere else
in the codebase (other AXAML files, code-behind, drawing code).

Controls and renderers expose `StyledProperty<IBrush?>` dependency properties
and the consumer AXAML binds via `{StaticResource ...}`. The flow is exactly:

```
AXAML StaticResource  →  StyledProperty<IBrush?>  →  draw / measure / parameter
```

`Application.Current.Resources` access from C# is forbidden. If you need a
brush in code, expose it through a styled property and bind it from XAML.

## Themes

Three themes ship: Light (default), Dark, System-follows-OS. Theme switching
takes effect at runtime — no restart. Theme-dependent resources are split
into a default brush set and `<Style>` selectors that swap brushes when the
container's `RequestedThemeVariant` changes.

## Resources and styling

- Shared text styles, icon templates, padding scales, and animation easings
  live in `App.axaml` or in dedicated dictionaries merged in from `Resources/`.
- Per-View styles live in the View's AXAML. Per-component styles that span
  multiple Views live in a shared dictionary.
- `<Style Selector="…">` selectors must be precise — broad type selectors
  (`Selector="Button"`) leak across the app and cause visual regressions.

## Bindings

- Use compiled bindings — set `x:DataType` on the root element so binding paths
  are validated at compile time.
- Provide `FallbackValue` / `TargetNullValue` on any nullable path.
- One-way bindings by default; two-way only when the View must write back
  (`TextBox.Text` to a ViewModel string, `TimePicker.SelectedTime`, …).
- Bindings against properties that do **not** raise `INotifyPropertyChanged`
  will fail silently at runtime — wrap them in `[ObservableProperty]` (see
  `mvvm.instructions.md`).
- Casing in `{Binding}` paths must match the property exactly; the binding
  diagnostics in `Avalonia.Diagnostics` (DevTools) surfaces typos.

## Virtualization

Large collections (traffic-flow list, scripting diagnostics, certificate
table) **must** use `ItemsControl` with a `VirtualizingStackPanel`. Without
virtualization, the UI thread allocates one container per row at startup and
the app stalls when the user opens a 100,000-flow session.

## Threading

Avalonia bindings and visual-tree access are UI-thread only. After awaiting a
non-UI task in a code-behind handler, hop back through
`Dispatcher.UIThread.Post(…)` before touching a control. See
`mvvm.instructions.md` for the matching ViewModel rules.

## Behaviors

`Behavior<T>` derivatives are the only home for code that needs typed control
access:

- Override `OnAttached` (or `OnAttachedToVisualTree`) and pair every event
  subscription with an `OnDetached` (or `OnDetachedFromVisualTree`)
  unsubscription.
- Do not hold a strong reference to the associated control beyond the attach
  window — Avalonia recycles controls in `ItemsControl`.
- Behaviors are unit-tested by attaching them to a minimal control fixture
  in the matching `*.Tests` project; see existing examples under
  `Presentation.*/Behaviors/`.

## Accessibility (WCAG 2.1 AA)

- Every interactive control has a keyboard equivalent.
- `AutomationProperties.Name` and `HelpText` are set for screen-reader use.
- Focus moves logically; tab order matches reading order.
- Colour contrast is sourced from the theme — never hard-code a foreground
  that clashes with the theme's background.

## Resource dictionaries

When adding a new resource:

1. Decide its scope (global → `App.axaml`; feature → dictionary inside the
   feature's `Presentation.<X>` project).
2. Add the key, run a build, and confirm `Test-ResourceKeys.ps1` still passes
   (`Invoke-Build.ps1` runs it automatically).
3. Reference the resource through `{StaticResource Key}` — never through
   `DynamicResource` unless the value genuinely changes at runtime.

## Diagnostics

`Avalonia.Diagnostics` (the DevTools overlay) is wired in Debug builds via the
`Client` host. When investigating layout, binding, or visual issues, open
DevTools (`F12` by default), use the visual-tree panel for layout questions,
and the binding panel for failed-binding diagnostics.
