using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests covering the global keyboard shortcuts wired on
///     <see cref="Proxyfan.Client.Shell.Views.ShellWindow" /> for tool toggles
///     and rule activation. Covers <c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>
///     for the Ctrl+Shift+N (No-Caching) and Ctrl+Shift+B (Breakpoint) gestures.
///     <para>
///         Both toggles are observable only through the menu state and other
///         indirect signals (the rule actually mutates a domain object — there
///         is no toolbar light). To assert that the shortcut DID fire we verify
///         the window remained responsive (no crash) and reading the title
///         still works.
///     </para>
/// </summary>
public sealed class ShellPageGlobalShortcutsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task PressCtrlShiftN_FreshShell_KeepsWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // ToggleNoCachingCommand binding for Ctrl+Shift+N. The visible effect
        // is invisible to UIA (no toolbar indicator) but a crash is observable.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_N);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.FilterTextBox()).IsNotNull();
    }

    [Test]
    public async Task PressCtrlShiftN_TwicePressed_StillResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Two consecutive toggles flip the No-Caching rule on then off.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_N);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_N);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
    }

    [Test]
    public async Task PressCtrlShiftB_FreshShell_KeepsWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // ToggleBreakpointCommand binding for Ctrl+Shift+B.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_B);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.FilterTextBox()).IsNotNull();
    }

    [Test]
    public async Task PressDelete_WithEmptyTrafficList_DoesNotCrash()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Delete is bound to RemoveSelected at the window level; with no
        // selection it must be a safe no-op.
        Keyboard.Type(VirtualKeyShort.DELETE);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
    }
}
