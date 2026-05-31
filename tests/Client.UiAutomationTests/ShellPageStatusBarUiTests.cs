using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests covering the shell status bar
///     (<c>docs/DESIGN.md § 4.7 Status Bar</c>), the source list panel
///     (<c>docs/DESIGN.md § 4.2 Source List Panel</c>), the tab close button
///     (<c>docs/DESIGN.md § 6.25 Multiple Tabs</c>), and the additional
///     keyboard shortcuts wired on the shell window
///     (<c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>: Ctrl+T, Ctrl+W,
///     Ctrl+Shift+N, Ctrl+Shift+B). Every test launches a fresh, sandboxed
///     <c>Client.Desktop.exe</c> and drives the live UI through FlaUI.
/// </summary>
public sealed class ShellPageStatusBarUiTests : UiAutomationTestBase
{
    [Test]
    public async Task StatusBar_FreshLaunch_ShowsZeroFlows()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The status bar renders the literal string "0" right after the
        // "Flows captured" label on a fresh shell.
        await Assert.That(shell.HasVisibleText("0")).IsTrue();
    }

    [Test]
    public async Task StatusBar_AfterClickingPauseCapture_ShowsCapturePausedIndicator()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Pause Capture").Click();
        shell.WaitUntil(
            () => shell.HasVisibleText("Capture paused"),
            description: "Capture paused indicator visible in status bar");

        await Task.CompletedTask;
    }

    [Test]
    public async Task StatusBar_AfterResumingCapture_HidesCapturePausedIndicator()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Pause Capture").Click();
        shell.WaitUntil(() => shell.HasVisibleText("Capture paused"), "paused");

        shell.ToolbarButton("Resume Capture").Click();
        shell.WaitUntil(
            () => !shell.HasVisibleText("Capture paused"),
            description: "Capture paused indicator gone after resume");

        await Task.CompletedTask;
    }

    [Test]
    public async Task SourceList_FreshLaunch_HasAtLeastOneEntryForAllGroup()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The synthetic "All" group renders a TextBlock with the literal "All"
        // string inside the SourceList container. The DataTemplate inside the
        // Avalonia ListBox doesn't surface as UIA ListItem on every framework
        // build, so assert via the visible text instead — that's also what a
        // screen reader user perceives.
        shell.WaitUntil(
            () => shell.HasVisibleText("All"),
            description: "All-group label visible in source list");

        await Task.CompletedTask;
    }

    [Test]
    public async Task CloseTabButton_OnSecondTab_RemovesIt()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Add a tab first — the default first tab is sticky and renders no close button.
        shell.NewTabButton().Click();
        WaitForTabCount(shell, 2, "second tab present");
        shell.WaitUntil(() => shell.CloseTabButtons().Length >= 1, "close button visible on added tab");

        var closeButton = shell.CloseTabButtons()[0];
        closeButton.Click();

        WaitForTabCount(shell, 1, "tab strip back to 1");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PressCtrlT_FreshShell_AddsNewTab()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();
        var before = SafeTabCount(shell);

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_T);

        WaitForTabCount(shell, before + 1, $"tab strip grew to {before + 1} after Ctrl+T");
        await Task.CompletedTask;
    }

    [Test]
    public async Task PressCtrlK_WithEmptyTrafficList_DoesNotCrash()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Ctrl+K is the global ClearCommand binding; the empty-list case must not error.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_K);

        // Window is still responsive after the gesture.
        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
    }

    private static int SafeTabCount(ShellPage shell)
    {
        // The tab strip can transiently throw E_UNEXPECTED while a Click is
        // updating its children. Retry a few times.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return shell.TabList().FindAllDescendants(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).Length;
            }
            catch
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        return shell.TabList().FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).Length;
    }

    private static void WaitForTabCount(ShellPage shell, int expected, string description)
    {
        shell.WaitUntil(() => SafeTabCount(shell) == expected, description);
    }
}
