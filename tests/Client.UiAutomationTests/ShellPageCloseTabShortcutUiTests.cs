using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Ctrl+W close-active-tab keyboard
///     shortcut wired on the shell (<c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>).
///     Ctrl+W invokes <c>TabHost.CloseActiveTabCommand</c>; the default first
///     tab is sticky and cannot be closed, so a fresh shell remains at one
///     tab after Ctrl+W and the shell remains responsive.
/// </summary>
public sealed class ShellPageCloseTabShortcutUiTests : UiAutomationTestBase
{
    [Test]
    public async Task PressCtrlW_OnDefaultTabOnly_ShellRemainsResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // The default first tab is sticky (no close button rendered), so
        // Ctrl+W on a fresh shell must be a safe no-op.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task PressCtrlW_TwiceInARow_KeepsShellResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Multiple presses of Ctrl+W on the sticky default tab must remain
        // a safe no-op without crashing the shell.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);
        System.Threading.Thread.Sleep(100);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Pause Capture").IsEnabled).IsTrue();
    }
}
