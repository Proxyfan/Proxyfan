# Proxyfan — UI Automation Tests (FlaUI, real desktop process)

True end-to-end UI automation tests that launch a **real** `Client.Desktop.exe`
process and drive it through the Windows UI Automation provider using
[FlaUI](https://github.com/FlaUI/FlaUI). You can watch the app appear,
have its buttons clicked, menus opened, and text typed by the test harness,
then close when the test finishes.

These are deliberately **separate** from
`tests/Client.EndToEndTests/` (which uses `Avalonia.Headless`). FlaUI tests
exercise the real Windows compositor, the real input pipeline, the real DI
host startup, real fonts, real theming — everything a user actually sees.

## Running

```powershell
# Excluded by default — the CI / release pipelines never run these.
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Run-Tests.ps1 -IncludeEndToEnd

# Just this project, fastest iteration loop.
dotnet test --project tests/Client.UiAutomationTests/Client.UiAutomationTests.csproj -c Debug
```

**Watch the screen while these run.** A real Proxyfan window flashes up for
~1.7 s per test, the toolbar buttons get clicked, menus drop open, and the
window closes between tests.

## Per-test isolation guarantees

Every test launches a freshly spawned `Client.Desktop.exe` with environment
variables that pin it to a hermetic sandbox:

| Variable | Value | Why |
|---|---|---|
| `LOCALAPPDATA` | per-test temp dir | App-state directory (`%LOCALAPPDATA%\Proxyfan`) starts empty — equivalent to a brand-new installation. Wiped on teardown. |
| `proxy__port` | per-test ephemeral high port | No conflict with the developer's real Proxyfan at 8080 or anything else listening. |
| `proxy__isAutoStart` | `false` by default | Don't open a TCP listener at all unless the test explicitly asks. |
| `proxy__isRegisterSystemProxy` | `false` | Never touches the real Windows Internet Settings system proxy registry. |
| `updates__isEnabled` | `false` | No background HTTP traffic to the update server during tests. |

The hermetic sandbox is what gives this suite the same observable starting
state as a fresh install, without the speed cost of running real MSIX
install/uninstall cycles per test.

## MSIX install/uninstall path — scaffolded, not wired

A scaffold for true MSIX install/uninstall-per-test is in place but not yet
hooked into the test harness:

| File | Purpose |
|------|---------|
| `installer/Proxyfan.appxmanifest` | Minimal `Package` manifest declaring `runFullTrust` capability (needed because Proxyfan binds raw TCP and writes HKCU). |
| `.tools/Build-MsixPackage.ps1` | Builds `Proxyfan-<version>-win-x64.msix` from a self-contained publish via `MakeAppx.exe`. Generates placeholder logos. **Requires the Windows 10 SDK 10.0.19041.0 or later** — install from <https://developer.microsoft.com/windows/downloads/windows-sdk/> if missing. |

To finish wiring MSIX into the test cycle (work for a follow-up session):

1. Install the Windows 10 SDK (provides `MakeAppx.exe` and `SignTool.exe`).
2. Create a self-signed cert with subject `CN=Proxyfan` and install it under
   `LocalMachine\TrustedPeople`.
3. Add a signing step to `Build-MsixPackage.ps1` calling `SignTool.exe`.
4. Extend `ProxyfanApp.Launch` with a `useMsix: true` mode that:
   - Builds + signs the MSIX once per assembly (cached by content hash).
   - Calls `Add-AppxPackage -Path <msix>` before each test.
   - Launches via `explorer.exe shell:AppsFolder\Proxyfan.Proxyfan_<hash>!App`
     instead of direct .exe.
   - Calls `Remove-AppxPackage` after teardown.
5. Per-test overhead climbs from ~2 s to ~60 s. Budget accordingly.

Until that's done, the current `LOCALAPPDATA`-redirection sandbox gives
**observably equivalent fresh-install state** for everything the UI exposes
(empty user config, no certificates, no system proxy registration). The only
things the LOCALAPPDATA sandbox does NOT exercise are: the MSIX package
extraction, the AUMID Start-Menu shortcut, and the signing/cert chain — all
distribution concerns rather than application behaviour.

## Architecture

| Component | Responsibility |
|-----------|----------------|
| `Infrastructure/ProxyfanApp.cs` | Launches `Client.Desktop.exe` under sandbox env vars, attaches a `UIA3Automation`, waits for the main window, brings it to the foreground, exposes `WaitForToolWindow(title)` for popup windows, cleans up on dispose. |
| `Infrastructure/ShellPage.cs` | Page-object wrapper with typed accessors (`FilterTextBox`, `SourceList`, `TabList`, `NewTabButton`, `ToolbarButton`, `MenuBar`, `CloseTabButtons`, `HasVisibleText`) + bounded `WaitUntil`/`WaitForRaw` polling. |
| `Infrastructure/UiAutomationTestBase.cs` | TUnit base class with `[NotInParallel]` (only one shell on screen at any moment) and a per-test 2-minute hard timeout. |
| `ProxyfanAppSmokeTests` | Single smoke test that proves the harness works (launch → find main window → close). |
| `ShellPageUiTests` | Toolbar Pause/Resume swap, Clear, New Tab, Ctrl+R, fresh-launch element discovery — all driven through real mouse / keyboard. |
| `ShellPageToolbarUiTests` | Backspace-clear filter, multi-toggle Pause/Resume cycle, every toolbar button is enabled on fresh shell. |
| `ShellPageMenuUiTests` | File/Tools/View menus open via mouse click, expected sub-items are present. |
| `ShellPageStatusBarUiTests` | Status bar flow count, Capture-paused indicator visibility, Source List All-group, tab close via X button, Ctrl+T new tab. |
| `ShellPageGlobalShortcutsUiTests` | Ctrl+Shift+N (No Caching), Ctrl+Shift+B (Breakpoint), Delete key — all routed through the real keyboard. |
| `ShellPageMultiTabAndFilterUiTests` | New tab clicked 3x grows strip by 3, filter retyping replaces old value, mixed-case preserved, focus returns after tab click, idempotent Clear keeps toolbar functional. |

## Why these tests can never run on CI

UI automation owns the global mouse cursor and active window for the duration
of each test. Running them inside an unattended CI agent would either:

- Steal focus from other processes if there is a real desktop session, or
- Time out trying to discover controls because there is no compositor at all
  (Windows CI runners don't always have one).

`Run-Tests.ps1` excludes any test project with
`<IsEndToEndTestProject>true</IsEndToEndTestProject>` by default; the CI
workflow never passes `-IncludeEndToEnd`.

## Determinism guarantees

- **One process at a time.** `[NotInParallel]` on the base class serialises
  every test in the assembly so there's only ever one Proxyfan window on
  screen.
- **Bounded waits.** `ShellPage.WaitUntil` polls a predicate up to 15 s by
  default and throws a meaningful `TimeoutException` instead of hanging.
- **Element discovery, not coordinate clicks.** All FlaUI interactions look
  up controls by accessibility name, then invoke patterns or click their
  centre, so window resizes and DPI scaling do not break the suite.
- **Hermetic sandbox.** Per-test `LOCALAPPDATA`, ephemeral proxy port, no
  system proxy registration, no auto-update probes.
- **Clean teardown.** `ProxyfanApp.DisposeAsync` closes (and force-kills if
  necessary) the spawned process and wipes its data directory before the
  next test starts.

If a new test starts flaking, check the four items above first.
