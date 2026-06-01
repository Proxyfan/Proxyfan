# Proxyfan — UI Automation Tests (FlaUI, MSIX install/uninstall per test)

True end-to-end UI automation tests that drive Proxyfan through Windows UI
Automation via [FlaUI](https://github.com/FlaUI/FlaUI). The **canonical mode**
runs every test through a full MSIX install → run → uninstall cycle: each
test starts from a brand-new fresh install of the signed MSIX package.

You can watch the app appear, have its buttons clicked, menus opened, and
text typed by the test harness, then close + uninstall when the test
finishes.

## Quick start

```powershell
# One-shot setup (elevated): installs Windows SDK, creates + trusts
# self-signed cert, builds + signs the MSIX. Idempotent.
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Initialize-MsixTestEnvironment.ps1

# Run the full MSIX-pipeline test suite. Each test: Add-AppxPackage ->
# launch via shell:AppsFolder\AUMID -> FlaUI assertions -> Remove-AppxPackage.
dotnet run --project tests/Client.UiAutomationTests --no-build -c Debug
```

The full **132-test** suite runs in **~36 minutes** (~16 s per test on
average — install/uninstall dominate fast tests, the longer interactions
amortise the overhead). Zero flakiness across the most recent full run
(132/132 passed, 35m 41s).

## The pipeline (what happens for every test)

Every individual test goes through this lifecycle:

| Step | What | Backed by |
|------|------|-----------|
| 1 | `Add-AppxPackage -Path Proxyfan-…-win-x64.msix` | `MsixInstaller.Install` |
| 2 | `explorer.exe shell:AppsFolder\Proxyfan.Proxyfan_<hash>!App` | `MsixInstaller.LaunchAndAttach` |
| 3 | FlaUI attaches to the spawned process, drives the UI | `ProxyfanApp` + `ShellPage` + `ToolWindowPage` |
| 4 | App closes (FlaUI `Application.Close` → `CloseMainWindow`) | `ProxyfanApp.DisposeAsync` |
| 5 | `Remove-AppxPackage` | `MsixInstaller.Uninstall` |

The MSIX itself is built + signed **once per test process** (cached). The
install and uninstall steps run per test.

`[NotInParallel]` on `UiAutomationTestBase` guarantees only one Proxyfan
window appears on screen at any moment.

## Opting out of MSIX cycle for fast iteration

Per-test MSIX cycles cost ~14–18 s overhead. For iterating on a new test you
can opt out and use direct `.exe` launch under env-var sandbox (~2–4 s/test;
the full suite drops to ~5 minutes):

```powershell
$env:PROXYFAN_UI_TESTS_SKIP_MSIX = 'true'
dotnet run --project tests/Client.UiAutomationTests --no-build -c Debug
```

Both modes share the same test code; the only difference is whether
`ProxyfanApp.Launch` goes through `LaunchViaMsix` or `LaunchViaDirectExe`.

## Architecture

| Component | Responsibility |
|-----------|----------------|
| `installer/Proxyfan.appxmanifest` | Minimal MSIX manifest, declares `runFullTrust` (Proxyfan binds raw TCP). |
| `.tools/Build-MsixPackage.ps1` | `dotnet publish` self-contained → stage layout → `MakeAppx pack` → unsigned `.msix`. |
| `.tools/Initialize-MsixTestEnvironment.ps1` | One-shot: install SDK, mint + trust dev cert, build + sign MSIX. |
| `Infrastructure/MsixInstaller.cs` | `Add-AppxPackage`/`Remove-AppxPackage` driver + AUMID-based `LaunchAndAttach`. |
| `Infrastructure/ProxyfanApp.cs` | Per-test orchestrator. Two modes (`LaunchViaMsix`, `LaunchViaDirectExe`) sharing FlaUI lifecycle. |
| `Infrastructure/ShellPage.cs` | Page-object: typed accessors (filter, source list, tabs, menus, toolbar buttons) + bounded polling + `OpenToolWindow` helper. |
| `Infrastructure/ToolWindowPage.cs` | Page-object: typed accessors for tool windows (`Button`, `TextBoxByName`, `ListBoxByName`, `ComboBoxByName`, `CheckBox`, `HasVisibleText`, `HasButton`) + close. |
| `Infrastructure/UiAutomationTestBase.cs` | TUnit base class with `[NotInParallel]` + per-test 2-minute hard timeout. |

### Shell-level test files

| File | Tests | Focus |
|---|---|---|
| `ProxyfanAppSmokeTests`                        | 1  | Single smoke test that proves the install/run/uninstall pipeline works end-to-end. |
| `ProxyfanAppWindowStateUiTests`                | 5  | Window bounds, keyboard focusability, reasonable size, responsiveness after rapid sequences. |
| `ShellPageUiTests`                             | 7  | Toolbar Pause/Resume swap, Clear, New Tab, Ctrl+R, fresh-launch element discovery. |
| `ShellPageToolbarUiTests`                      | 4  | Backspace-clear filter, multi-toggle Pause/Resume, every toolbar button is enabled. |
| `ShellPageMenuUiTests`                         | 3  | File/Tools/View menus open via mouse click, expected sub-items present. |
| `ShellPageStatusBarUiTests`                    | 7  | Status bar flow count, capture-paused indicator, source-list All-group, tab close via X, Ctrl+T. |
| `ShellPageGlobalShortcutsUiTests`              | 4  | Ctrl+Shift+N (No Caching), Ctrl+Shift+B (Breakpoint), Delete key. |
| `ShellPageSessionShortcutsUiTests`             | 1  | Ctrl+E (Save Session) on empty traffic — graceful no-op. |
| `ShellPageMultiTabAndFilterUiTests`            | 5  | Three-tab add growth, retype-replaces, mixed-case preserved, focus returns after tab click, idempotent triple-Clear. |
| `ShellPageExtendedUiTests`                     | 4  | "Sources" header label, "Proxyfan" app-name text, URL-syntax filter text preservation, regex-like filter text preserved verbatim. |
| `ShellPageFilterEdgeCasesUiTests`              | 3  | Filter preserves quotes/parens/slashes, full URLs with port + query string, 200-char long input. |
| `ShellPageTrafficListUiTests`                  | 2  | Flow grid is discoverable by automation name, has non-zero bounds. |
| `ShellPageInspectorUiTests`                    | 7  | Two tab controls present; Headers/Body/Raw/Summary/Query/Cookies/Auth/Timing tabs discoverable; click Headers/Body/Raw/Summary marks tab as selected. |
| `ShellPageTabActivationUiTests`                | 3  | Click first/second tab to activate it; Ctrl+T four times grows strip by 4. |
| `ShellPageToolWindowReopenUiTests`             | 2  | Re-opening a tool window from the menu activates the existing single instance (Preferences, Block List). |
| `ShellPageHostStateUiTests`                    | 3  | Update banner not visible by default; exactly one child window on fresh launch; menu bar has exactly 3 top-level menus (File, Tools, View). |

### Tool-window test files (one per top-level tool window)

| File | Tests | Tool window | Menu path |
|---|---|---|---|
| `ShellPagePreferencesUiTests`                  | 3  | Preferences            | File → Preferences... |
| `ShellPageBlockListUiTests`                    | 6  | Block List             | Tools → Block List... |
| `ShellPageAllowListUiTests`                    | 4  | Allow List             | Tools → Allow List... |
| `ShellPageBreakpointUiTests`                   | 5  | Breakpoint             | Tools → Breakpoint... |
| `ShellPageComposerUiTests`                     | 5  | Request Composer       | Tools → Compose Request... |
| `ShellPageScriptingUiTests`                    | 5  | Scripting              | Tools → Scripting... |
| `ShellPageDiffToolUiTests`                     | 2  | Diff Tool              | Tools → Diff Tool... |
| `ShellPageCustomColumnsUiTests`                | 2  | Custom Header Columns  | Tools → Custom Header Column... |
| `ShellPageMapLocalUiTests`                     | 4  | Map Local              | Tools → Map Local... |
| `ShellPageMapRemoteUiTests`                    | 4  | Map Remote             | Tools → Map Remote... |
| `ShellPageSecureSocketsLayerProxyingUiTests`   | 4  | SSL Proxying           | Tools → SSL Proxying... |
| `ShellPageCertificateManagerUiTests`           | 2  | Certificate Manager    | Tools → Certificate Manager... |
| `ShellPageDomainNameSystemSpoofingUiTests`     | 4  | DNS Spoofing           | Tools → DNS Spoofing... |
| `ShellPageReverseProxyUiTests`                 | 4  | Reverse Proxy          | Tools → Reverse Proxy... |
| `ShellPageRemoteDevicesUiTests`                | 2  | Remote Devices         | Tools → Remote Devices... |
| `ShellPageRemoteProcedureCallDescriptorsUiTests` | 2 | gRPC Descriptors       | Tools → gRPC Descriptors... |
| `ShellPageThrottleUiTests`                     | 4  | Network Throttle       | Tools → Throttle... |
| `ShellPageThemeUiTests`                        | 4  | Theme                  | View → Theme... |
| `ShellPageKeyboardShortcutsUiTests`            | 3  | Keyboard Shortcuts     | View → Keyboard Shortcuts... |
| `ShellPagePluginManagerUiTests`                | 2  | Plugin Manager         | View → Plugin Manager... |

**Total: 132 tests across 36 files.**

## Determinism guarantees

- **Fresh install per test.** `Add-AppxPackage` runs before each test; if a
  prior crash left the package installed, the next install detects + reinstalls.
- **One process at a time.** `[NotInParallel]` on the base class serialises
  every test.
- **Bounded waits.** `ShellPage.WaitUntil`, `ToolWindowPage.WaitUntil`, and
  `MsixInstaller.LaunchAndAttach` all poll with explicit timeouts; no hangs.
- **Element discovery, not coordinate clicks.** All FlaUI interactions look
  up controls by accessibility name, then invoke patterns or click their
  centre — window resizes and DPI scaling do not break the suite.
- **Avalonia menu invocation via UIA Invoke pattern.** `ShellPage.OpenToolWindow`
  opens menus via `ExpandCollapse` then fires sub-items via the `Invoke`
  pattern — clicking sub-items via mouse can race the popup dismissal, so
  the keyboard-less UIA path is used.
- **Tool window discovery via descendant traversal.** Avalonia tool windows
  opened with `Window.Show(owner)` surface as desktop descendants (owned
  popups), not direct desktop children. `WaitForToolWindow` walks the full
  descendant tree scoped by process id.
- **Tool window `Close()` always runs in `finally`.** Every tool-window
  test wraps its assertions so a failed assertion never leaves a tool
  window on screen for the next test.
- **Hermetic.** MSIX containerises user data per package, so each install
  starts with empty state. The MSIX never touches the actual system proxy
  registry value (we don't expose the system-proxy toggle to fire in tests).

## Why these tests can never run on CI

UI automation owns the global mouse and active window for the duration of
each test. CI agents typically lack a visible compositor and would deadlock
on element discovery, plus MSIX `Add-AppxPackage` requires the test machine
to trust the signing cert (which is a manual one-time step).

`Run-Tests.ps1` excludes any test project with
`<IsEndToEndTestProject>true</IsEndToEndTestProject>` by default; the CI
workflow never passes `-IncludeEndToEnd`.

## Coverage (DESIGN.md sections)

| Section | Status |
|---------|--------|
| § 4.1 Main Window | ✅ bounds, focusability, size, responsiveness |
| § 4.2 Source List Panel | ✅ Sources header, All-group visible |
| § 4.3 Traffic Flow List | ✅ DataGrid discoverable, non-zero bounds |
| § 4.4 Inspector Panel | ✅ Two tab controls (Request, Response); Headers/Body/Raw/Summary/Query/Cookies/Auth/Timing tabs; click-to-select round-trip on Headers/Body/Raw/Summary |
| § 4.5 Menu Bar | ✅ File, Tools, View dropdowns + sub-items |
| § 4.6 Toolbar | ✅ Pause/Resume/Clear/Open/Save/Enable Proxy + filter text box |
| § 4.7 Status Bar | ✅ flow count, capture-paused indicator |
| § 6.1 Traffic Capture | ✅ Pause/Resume cycles |
| § 6.4 Traffic Filtering | ✅ type, backspace clear, mixed case, retype, focus-return, regex syntax preservation, quotes/parens/slashes, full URL with port+query, 200-char long input |
| § 6.5 Map Local | ✅ open from menu, all response fields, add rule, status+reason values persist |
| § 6.6 Map Remote | ✅ open from menu, destination fields, add rule, PreserveHost checkbox toggle |
| § 6.7 Breakpoints | ✅ open from menu, phases combo, add pattern, Resume/Abort buttons, Enabled toggle, empty pause list |
| § 6.8 Scripting (C#) | ✅ open from menu, OnRequest/OnResponse boxes, Compile/Clear buttons, type into script, Enabled toggle, Clear active script no-op |
| § 6.9 Allow List | ✅ open from menu, add pattern, add+remove round-trip leaves list empty |
| § 6.10 Block List | ✅ open from menu, add pattern, Enabled checkbox toggle round-trip, add+remove round-trip, empty pattern guard |
| § 6.12 Network Throttling | ✅ open from menu, preset list, select preset, Apply leaves window responsive |
| § 6.14 Diff Tool | ✅ open from menu, Left/Right lists, Clear Pool |
| § 6.15 Repeat Request (Composer) | ✅ open from menu, method/URL/headers/body, type body+headers+URL preserved, Send/cURL buttons |
| § 6.19 gRPC Inspection (Descriptors) | ✅ open from menu, Load/Unload/Clear buttons |
| § 6.24 Custom Columns | ✅ open from menu, add column with display name + header key |
| § 6.25 Multiple Tabs | ✅ add via "+", add via Ctrl+T (×4), close via X, click-to-activate first/second tab, multi-add |
| § 6.26 Certificate Management | ✅ open from menu, Install/Uninstall/Export/Regenerate buttons, metadata labels |
| § 8 Theming and Appearance | ✅ open from menu, picker list, select theme, IsSelected round-trip, Apply leaves window responsive |
| § 9 Keyboard Shortcuts | ✅ Ctrl+R, Ctrl+K, Ctrl+T, Ctrl+E, Ctrl+Shift+N, Ctrl+Shift+B, Delete |
| § 11 Configuration and Preferences | ✅ open from menu, locale/theme/log-level, type into locale |
| § 12 Auto-Update | ✅ update banner NOT visible on fresh launch (negative test) |
| § 6.2 HTTPS Decryption (SSL Proxying tool window) | ✅ open from menu, enable checkbox + toggle, add pattern |
| DNS Spoofing tool window | ✅ open from menu, hostname/address, add entry, Enable All/Disable All/Refresh Counts buttons |
| Reverse Proxy tool window | ✅ open from menu, route fields, TLS mode combo, full add-route round-trip |
| Remote Devices tool window | ✅ open from menu, Rename/Disconnect/Forget buttons |
| Plugin Manager tool window | ✅ open from menu, Refresh/Reload/CheckForUpdates buttons |
| Keyboard Shortcuts tool window | ✅ open from menu, bindings list, Save/Reset buttons, Reset doesn't crash |
| Tool window single-instance behaviour | ✅ re-opening Preferences and Block List activates existing window |
| Shell host invariants | ✅ exactly 3 top-level menus (File/Tools/View); fresh launch shows exactly 1 child window |
| § 5 First-Run Experience | ⚠️ feature not yet implemented in app |
| § 6.2 HTTPS Decryption (real traffic) | ⚠️ requires live network traffic |
| § 6.16/6.17 Save/Load Session via file picker | ⚠️ OS file picker, not driveable from FlaUI without elevated privileges |
| § 6.18/6.20/6.21/6.22 protocol inspectors | ⚠️ require live WS/SSE/Protobuf/GraphQL |
| § 7 CLI mode | ⚠️ different process / different test harness |

The ⚠️ rows are either not yet implemented in the app, or require modal /
OS-level dialog interaction that the FlaUI harness cannot drive
deterministically inside an MSIX-container sandbox.
