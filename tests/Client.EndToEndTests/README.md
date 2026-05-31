# Proxyfan — End-to-End UI Tests

Headless Avalonia UI tests that exercise `ShellWindow` and its child views with
real Avalonia rendering, input simulation, and binding evaluation. Backed by
**Avalonia.Headless 11.3** + **TUnit** + **Microsoft.Testing.Platform**.

These tests are **excluded from CI by default** (see the
`<IsEndToEndTestProject>true</IsEndToEndTestProject>` MSBuild property in
`Client.EndToEndTests.csproj`). The repo-level
[`.tools/Run-Tests.ps1`](../../.tools/Run-Tests.ps1) reads that property and
skips this project unless `-IncludeEndToEnd` is passed.

## Running the suite

```powershell
# Default: this project is skipped.
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1

# Opt in locally.
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -IncludeEndToEnd

# Run just this project, fastest iteration loop.
dotnet test --project tests/Client.EndToEndTests/Client.EndToEndTests.csproj -c Debug

# Run a single test method.
dotnet run --project tests/Client.EndToEndTests `
    -- --filter "ShellWindowSmokeEndToEndTests.Show_FreshEnvironment_RendersTitleAndPrimaryPanels"
```

## Architecture

| Component | Responsibility |
|-----------|----------------|
| `Infrastructure/TestApp.cs` | Minimal Avalonia `Application` — installs FluentTheme; does NOT boot the production DI host, `ProxyServer`, plugin loader, or update checker. |
| `Infrastructure/TestAppBuilder.cs` | Configures the headless `AppBuilder` (`UseHeadlessDrawing = true`). |
| `AssemblyInfo.cs` | `[AvaloniaTestApplication(typeof(TestAppBuilder))]` enables `HeadlessUnitTestSession.GetOrStartForAssembly`. |
| `Infrastructure/EndToEndTestBase.cs` | Base class providing `RunOnUiThreadAsync(...)` (dispatches onto the headless UI thread) and a 30-second per-test timeout. |
| `Infrastructure/TestShellEnvironment.cs` | Per-test wiring: builds a fully stubbed `ShellViewModel`, registers `LocalizationService` (so `{localization:Localize}` markup resolves), publishes the container via `ContainerLocator`, and shows `ShellWindow`. Disposing resets global state. |
| `Infrastructure/UiTreeFinder.cs` | Visual + logical tree walker for `FindByAutomationName`, `FindByName`, `FindAll`. |
| `Pages/ShellPage.cs` | Page-object wrapper around the live shell with typed accessors (`Menu()`, `SourceList()`, `FilterTextBox()`, …) and input helpers (`PressKey`, `TypeText`). |
| `Fixtures/EndToEndTrafficFlowFactory.cs` | Deterministic factories for `TrafficFlow` test data with stable IDs and timestamps. |
| `Stubs/Imported/` | Linked from `Client.Tests/Stubs/` — re-uses the battle-tested `ShellViewModelFactory`, `StubSystemProxy`, `StubToolWindowOpener`, `StubOptionsMonitor`, `InlineUserInterfaceScheduler` rather than duplicating them. |

## Adding a new test

1. Pick the requirement(s) from `docs/DESIGN.md` you intend to cover and pick
   the *single* type most under test — usually a `*ViewModel` or `ShellWindow`.
2. Create a file named `{TypeUnderTest}{Qualifier}EndToEndTests.cs`. The TUnit
   analyzer (`ATXTST002`) requires the class name to start with a type that
   exists in the codebase.
3. Inherit from `EndToEndTestBase`.
4. Each test method must follow `{Method}_{Scenario}_{ExpectedResult}` (three
   underscore-separated parts) per `ATXTST003`.
5. Wrap all UI work in `await RunOnUiThreadAsync(async () => { … })`.
6. Construct `using var env = new TestShellEnvironment();` per test — never
   share environments across tests, they own global UI state.
7. Use `ShellPage` for high-level interactions; drop down to `UiTreeFinder`
   only when the page object lacks a helper.
8. Assert against view-model state for behaviour and against
   `UiTreeFinder.FindAll<T>(...)` for visual structure.

## Test styles in this project

This project intentionally combines **two complementary test styles** that both
boot a real headless Avalonia window and bind the production ViewModels:

1. **UI-automation tests** — drive the application exclusively through Avalonia's
   real input pipeline (mouse down/up, key press/release, text input) and
   observe the resulting application state. These are what most testers mean
   by "end-to-end UI tests".
   - `ShellWindowMouseAndKeyboardAutomationEndToEndTests`
   - `ShellWindowMenuAutomationEndToEndTests`
   - `ShellWindowKeyboardShortcutsEndToEndTests`
   - `ShellWindowKeyboardShortcutsExtraEndToEndTests`
2. **VM-driven behaviour tests** — boot the real window, then invoke commands
   and properties on the bound ViewModels to assert behaviour. These exercise
   the full XAML binding + DI stack while being faster and more deterministic
   than full input simulation for combinatorial scenarios (e.g. every menu
   command, every preset, every annotation field). All other test files in
   this project.

## Test styles in this project

This project intentionally combines **two complementary test styles** that both
boot a real headless Avalonia window and bind the production ViewModels:

1. **UI-automation tests** — drive the application exclusively through Avalonia's
   real input pipeline (mouse down/up, key press/release, text input) and
   observe the resulting application state. These are what most testers mean
   by "end-to-end UI tests".
   - `ShellWindowMouseAndKeyboardAutomationEndToEndTests`
   - `ShellWindowMenuAutomationEndToEndTests`
   - `ShellWindowKeyboardShortcutsEndToEndTests`
   - `ShellWindowKeyboardShortcutsExtraEndToEndTests`
2. **VM-driven behaviour tests** — boot the real window, then invoke commands
   and properties on the bound ViewModels to assert behaviour. These exercise
   the full XAML binding + DI stack while being faster and more deterministic
   than full input simulation for combinatorial scenarios (e.g. every menu
   command, every preset, every annotation field). All other test files in
   this project.

Together they give defence-in-depth: input-pipeline regressions are caught by
the automation tests; combinatorial regressions are caught by the behaviour
tests.

## What is and isn't covered

The current suite covers `docs/DESIGN.md`:

- **§ 4 Application Layout** — main window title, three-panel structure, menu,
  toolbar, filter box, tab list, grid splitters.
- **§ 4.2 Source List Panel** — All group sentinel, selection narrows traffic via
  `HostFilter`, host group propagation.
- **§ 4.5 Menu Bar** — every Tools-menu and View-menu command routes to the
  correct `IToolWindowOpener` method.
- **§ 4.6 Toolbar** — system-proxy toggle command registers/unregisters and
  flips `IsSystemProxyEnabled`.
- **§ 4.7 Status Bar** — flow count, capture-paused indicator visibility.
- **§ 6.1 Traffic Capture** — capture starts on, toggle pauses/resumes, load
  flows populates the list, clear empties it, empty input is handled.
- **§ 6.3 Traffic Inspection** — selected flow state, selection cleared on
  removal.
- **§ 6.4 Traffic Filtering** — toolbar filter propagates to the VM, narrows
  visible flows, whitespace = no filter, clear restores, host-filter narrows
  to a single host.
- **§ 6.5 Map Local** — enable/disable, add entry with status / reason / headers
  / body, remove entry, reject non-numeric or out-of-range status codes,
  whitespace-only pattern is no-op, editor resets after add.
- **§ 6.6 Map Remote** — enable/disable, add entry with scheme/host/port/path/
  preserve-host toggle, remove entry, reject non-numeric port, whitespace-only
  pattern is no-op, host-only entries leave other fields null.
- **§ 6.7 Breakpoints** — global toggle reflects in both VM property and
  `MutableBreakpointConfiguration`.
- **§ 6.9 Allow List** — enable/disable, add pattern, remove pattern,
  whitespace-only input is no-op.
- **§ 6.10 Block List** — enable/disable, add pattern, remove pattern,
  whitespace-only input is no-op.
- **§ 6.11 No Caching** — global toggle reflects in both VM property and
  `MutableNoCachingRule`.
- **§ 6.12 Network Throttling** — seven presets (Off, 2G, 3G, 4G, WiFi, Bad
  Network, 100% Loss); Apply propagates to `MutableThrottleProfile`; external
  profile change mirrors back; Off disables; no-selection no-op.
- **§ 6.16 Session Management** — `SaveSession` invokes file picker + HAR
  exporter; cancelled picker skips export; empty traffic list still exports.
- **§ 6.17 Export and Import** — `OpenSession` invokes file picker + HAR
  importer; imported flows load into the traffic list.
- **§ 6.23 Color Tags and Comments** — per-flow color tag and comment
  mutation via `TrafficFlowViewModel`.
- **§ 6.25 Multiple Tabs** — fresh shell has one tab, add appends, active index
  follows new tab, close-active and close-by-instance commands.
- **§ 8 Theming and Appearance** — System/Light/Dark options, apply switches
  `ThemeService.CurrentTheme`, external theme change mirrors back.
- **§ 9 Keyboard Shortcuts** — `Ctrl+R` (toggle capture), `Ctrl+K` (clear),
  `Ctrl+T` (new tab), `Ctrl+W` (close active tab), `Delete` (remove selected),
  `Ctrl+Shift+N` (no-caching), `Ctrl+Shift+B` (breakpoint) all routed through
  real Avalonia input pipeline; plus the customization tool (one row per
  action, conflict detection, rebind, Save persists, Reset restores defaults).
- **§ 12 Auto-Update** — banner appears on `MutableUpdateNotification.Publish`,
  shows the right message + download URL, dismiss hides it, republishing the
  same version stays dismissed, publishing a newer version re-raises, `Clear`
  hides it.

**Not yet covered** (work for future sessions, see `docs/DESIGN.md` for the
list): § 5 first-run experience, § 6.2 HTTPS decryption, § 6.8 Scripting,
§ 6.13 Upstream Proxy Chaining, § 6.14 Diff Tool, § 6.15 Repeat Request,
§ 6.18–6.22 protocol inspectors, § 6.24 Custom Columns, § 6.26 Certificate
Management, § 7 CLI mode, § 10 Accessibility, § 11 Preferences, § 13 Privacy,
§ 15 Error Handling, § 16 i18n. Each future session should extend one or two
feature areas at production quality rather than dozens at low quality.

## Determinism guarantees

- `HeadlessUnitTestSession.GetOrStartForAssembly` creates a single UI thread
  for the whole assembly so dispatcher state is consistent across tests.
- `[NotInParallel(nameof(EndToEndTestBase))]` on the base class serializes
  every test in this assembly so the shared UI thread is never multiplexed.
- `TestShellEnvironment` resets `ContainerLocator` on dispose so no state
  leaks between tests.
- `EndToEndTrafficFlowFactory` builds flows with seeded GUIDs and a fixed
  base time — no `DateTimeOffset.UtcNow`, no `Guid.NewGuid()`.
- No network, no file I/O, no real `ProxyServer`.

If a new test starts flaking, check the four items above first.
