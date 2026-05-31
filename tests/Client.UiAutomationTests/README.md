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

The full 36-test suite runs in **~10 minutes** (~18 s per test, dominated
by the MSIX install/uninstall overhead). Zero flakiness across consecutive runs.

## The pipeline (what happens for every test)

Every individual test goes through this lifecycle:

| Step | What | Backed by |
|------|------|-----------|
| 1 | `Add-AppxPackage -Path Proxyfan-…-win-x64.msix` | `MsixInstaller.Install` |
| 2 | `explorer.exe shell:AppsFolder\Proxyfan.Proxyfan_<hash>!App` | `MsixInstaller.LaunchAndAttach` |
| 3 | FlaUI attaches to the spawned process, drives the UI | `ProxyfanApp` + `ShellPage` |
| 4 | App closes (FlaUI `Application.Close` → `CloseMainWindow`) | `ProxyfanApp.DisposeAsync` |
| 5 | `Remove-AppxPackage` | `MsixInstaller.Uninstall` |

The MSIX itself is built + signed **once per test process** (cached). The
install and uninstall steps run per test.

`[NotInParallel]` on `UiAutomationTestBase` guarantees only one Proxyfan
window appears on screen at any moment.

## Opting out of MSIX cycle for fast iteration

Per-test MSIX cycles cost ~18 s overhead. For iterating on a new test you
can opt out and use direct `.exe` launch under env-var sandbox (~2 s/test):

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
| `Infrastructure/ShellPage.cs` | Page-object: typed accessors (`FilterTextBox`, `SourceList`, `TabList`, `NewTabButton`, `ToolbarButton`, `MenuBar`, `CloseTabButtons`, `HasVisibleText`) + bounded polling. |
| `Infrastructure/UiAutomationTestBase.cs` | TUnit base class with `[NotInParallel]` + per-test 2-minute hard timeout. |
| `ProxyfanAppSmokeTests` | Single smoke test that proves the install/run/uninstall pipeline works. |
| `ShellPageUiTests` | Toolbar Pause/Resume swap, Clear, New Tab, Ctrl+R, fresh-launch element discovery. |
| `ShellPageToolbarUiTests` | Backspace-clear filter, multi-toggle Pause/Resume, every toolbar button is enabled. |
| `ShellPageMenuUiTests` | File/Tools/View menus open via mouse click, expected sub-items present. |
| `ShellPageStatusBarUiTests` | Status bar flow count, capture-paused indicator, source-list All-group, tab close via X, Ctrl+T. |
| `ShellPageGlobalShortcutsUiTests` | Ctrl+Shift+N (No Caching), Ctrl+Shift+B (Breakpoint), Delete key. |
| `ShellPageMultiTabAndFilterUiTests` | Three-tab add growth, retype-replaces, mixed-case preserved, focus returns after tab click, idempotent triple-Clear. |
| `ProxyfanAppWindowStateUiTests` | Window bounds, keyboard focusability, reasonable size, responsiveness after rapid sequences. |

## Determinism guarantees

- **Fresh install per test.** `Add-AppxPackage` runs before each test; if a
  prior crash left the package installed, the next install detects + reinstalls.
- **One process at a time.** `[NotInParallel]` on the base class serialises
  every test.
- **Bounded waits.** `ShellPage.WaitUntil` and `MsixInstaller.LaunchAndAttach`
  both poll with explicit timeouts; no hangs.
- **Element discovery, not coordinate clicks.** All FlaUI interactions look
  up controls by accessibility name, then invoke patterns or click their
  centre — window resizes and DPI scaling do not break the suite.
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
| § 4.2 Source List Panel | ✅ All-group visible |
| § 4.5 Menu Bar | ✅ File, Tools, View dropdowns + sub-items |
| § 4.6 Toolbar | ✅ Pause/Resume/Clear/Open/Save/Enable Proxy |
| § 4.7 Status Bar | ✅ flow count, capture-paused indicator |
| § 6.1 Traffic Capture | ✅ Pause/Resume cycles |
| § 6.4 Traffic Filtering | ✅ type, backspace clear, mixed case, retype, focus-return |
| § 6.25 Multiple Tabs | ✅ add via "+", add via Ctrl+T, close via X, multi-add |
| § 9 Keyboard Shortcuts | ✅ Ctrl+R, Ctrl+K, Ctrl+T, Ctrl+Shift+N, Ctrl+Shift+B, Delete |
| § 5 First-Run Experience | ❌ requires modal interaction patterns |
| § 6.2 HTTPS Decryption | ❌ requires real network traffic flowing |
| § 6.5–6.13 modification tools | ❌ require captured flows |
| § 6.14–6.17 diff/repeat/session/export | ❌ require traffic + file pickers |
| § 6.18–6.22 protocol inspectors | ❌ require live WS/gRPC/SSE/Protobuf/GraphQL |
| § 6.23/6.24/6.26 color/columns/certs | ❌ |
| § 7 CLI mode | ❌ different process / different test harness |
| § 10 A11y · § 11 Preferences · § 13 Privacy · § 15 Error Handling · § 16 i18n | ❌ |

Each ❌ section needs a separate body of work; many require traffic-
generation harnesses or modal-dialog FlaUI patterns that don't exist yet.
