using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for keyboard-driven menu interaction
///     (<c>docs/DESIGN.md § 4.5 Menu Bar</c>, § 9 Keyboard Shortcuts). The
///     View menu header is declared as <c>_View</c> in the resx so the
///     underscore activates an Alt+V mnemonic on Avalonia; this file covers
///     menu-via-keyboard scenarios that don't open a tool window
///     (those flows are exercised by the dedicated tool-window tests).
/// </summary>
public sealed class ShellPageMenuKeyboardUiTests : UiAutomationTestBase
{
    [Test]
    public async Task PressEscape_OnFreshShell_LeavesShellResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Escape with no open menu/popup must be a safe no-op.
        Keyboard.Type(VirtualKeyShort.ESCAPE);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task PressF10_FreshShell_LeavesShellResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // F10 traditionally activates the menu bar on Windows. Avalonia may
        // or may not honour this on every build, but pressing it must never
        // crash the shell.
        Keyboard.Type(VirtualKeyShort.F10);
        System.Threading.Thread.Sleep(150);
        // Dismiss any state Avalonia entered (selected menu, etc.).
        Keyboard.Type(VirtualKeyShort.ESCAPE);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
    }

    [Test]
    public async Task EscapeAfterFileMenuOpen_FreshShell_DismissesMenu()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Click File menu via accessible name to open it, then Escape to
        // dismiss. The shell should remain interactive afterwards.
        var fileMenu = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)))
            ?? throw new System.InvalidOperationException("File menu not found.");
        var expandCollapse = fileMenu.AsMenuItem().Patterns.ExpandCollapse.PatternOrDefault;
        if (expandCollapse is not null)
        {
            expandCollapse.Expand();
        }
        else
        {
            fileMenu.AsMenuItem().Click();
        }
        System.Threading.Thread.Sleep(300);

        shell.Window.Focus();
        Keyboard.Type(VirtualKeyShort.ESCAPE);
        System.Threading.Thread.Sleep(150);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Pause Capture").IsEnabled).IsTrue();
    }
}
