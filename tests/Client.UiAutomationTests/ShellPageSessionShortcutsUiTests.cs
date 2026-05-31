using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Ctrl+E session-save keyboard shortcut
///     wired on the shell (<c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>).
///     Ctrl+E invokes <c>SaveSessionCommand</c>; with no traffic captured
///     the command silently exits without opening the OS file picker, so we
///     only assert the shell remains responsive after the gesture.
/// </summary>
public sealed class ShellPageSessionShortcutsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task PressCtrlE_EmptySession_LeavesShellResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_E);

        // The shell must still respond and the toolbar must still be intact
        // after the (no-op on empty traffic) save attempt.
        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }
}
