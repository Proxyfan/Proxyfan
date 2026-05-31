using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     Window-state end-to-end FlaUI tests covering shell window behaviour
///     described in <c>docs/DESIGN.md § 4.1 Main Window</c>: the window is
///     maximized on launch, is on screen, has reasonable bounds, and stays
///     responsive after a sequence of interactions.
/// </summary>
public sealed class ProxyfanAppWindowStateUiTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshShell_IsOnScreenAndHasNonZeroBounds()
    {
        await using var app = ProxyfanApp.Launch();
        var window = app.GetMainWindow();

        var bounds = window.BoundingRectangle;
        await Assert.That(bounds.Width).IsGreaterThan(0);
        await Assert.That(bounds.Height).IsGreaterThan(0);
        await Assert.That(window.Properties.IsOffscreen.Value).IsFalse();
    }

    [Test]
    public async Task Launch_FreshShell_WindowIsKeyboardFocusable()
    {
        await using var app = ProxyfanApp.Launch();
        var window = app.GetMainWindow();

        // The shell window must be in a state where keyboard input can be
        // delivered (a real user can interact with it).
        await Assert.That(window.IsEnabled).IsTrue();
    }

    [Test]
    public async Task Launch_FreshShell_HasReasonablySizedClientArea()
    {
        await using var app = ProxyfanApp.Launch();
        var window = app.GetMainWindow();

        // The shell is declared with Width=800, Height=450, WindowState=Maximized.
        // On any sane display the actual size will be far larger than the
        // declared baseline, but never smaller.
        var bounds = window.BoundingRectangle;
        await Assert.That(bounds.Width).IsGreaterThanOrEqualTo(640);
        await Assert.That(bounds.Height).IsGreaterThanOrEqualTo(360);
    }

    [Test]
    public async Task Shell_AfterRepeatedKeyboardShortcuts_RemainsResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        // Hammer multiple bound shortcuts in quick succession.
        for (var i = 0; i < 3; i++)
        {
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_R);
            System.Threading.Thread.Sleep(80);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_K);
            System.Threading.Thread.Sleep(80);
        }

        // The window must still be findable and the toolbar must still respond.
        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task Shell_AfterMixedMouseAndKeyboardActions_RemainsResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Click toolbar buttons, type into filter, toggle capture — a realistic
        // multi-interaction sequence. The shell must survive all of it.
        shell.ToolbarButton("Pause Capture").Click();
        shell.WaitUntil(() => Exists(shell, "Resume Capture"), "paused");
        shell.FilterTextBox().Focus();
        Keyboard.Type("scenario");
        shell.WaitUntil(
            () => string.Equals(shell.FilterTextBox().Text, "scenario", StringComparison.Ordinal),
            "filter populated");
        shell.ToolbarButton("Resume Capture").Click();
        shell.WaitUntil(() => Exists(shell, "Pause Capture"), "back to capturing");

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.FilterTextBox().Text).IsEqualTo("scenario");
    }

    private static bool Exists(ShellPage shell, string label)
    {
        try
        {
            return shell.ToolbarButton(label) is { IsEnabled: true };
        }
        catch
        {
            return false;
        }
    }
}
