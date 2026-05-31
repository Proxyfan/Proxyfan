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
///     End-to-end FlaUI tests covering additional shell user-interaction
///     patterns from <c>docs/DESIGN.md § 4.6 Toolbar</c>, § 4.2 Source List
///     Panel, and § 6.4 Traffic Filtering. Every test goes through the full
///     MSIX install → run → uninstall pipeline.
/// </summary>
public sealed class ShellPageExtendedUiTests : UiAutomationTestBase
{
    [Test]
    public async Task SourcePanelHeader_FreshShell_IsVisibleWithSourcesLabel()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The left source panel renders a "Sources" header label above the listbox.
        shell.WaitUntil(
            () => shell.HasVisibleText("Sources"),
            description: "Sources header visible in left panel");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ToolbarAppName_FreshShell_DisplaysProxyfanText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The toolbar's left-most slot renders the literal app name "Proxyfan".
        shell.WaitUntil(
            () => shell.HasVisibleText("Proxyfan"),
            description: "Proxyfan app-name text visible in toolbar");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypedUrlPattern_PreservesSlashesAndDots()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("https://api.example.com/users/42");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "https://api.example.com/users/42", StringComparison.Ordinal),
            description: "filter preserves URL syntax characters");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypedRegexLikePattern_NotInterpretedAsRegex()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type(".*example");

        // We only assert the textbox preserves what was typed — the filter
        // semantics (substring vs regex) is a separate VM concern.
        shell.WaitUntil(
            () => string.Equals(filter.Text, ".*example", StringComparison.Ordinal),
            description: "filter preserves regex-like syntax characters verbatim");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ToolbarOpenSessionButton_Clicked_DoesNotCrash()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Clicking Open Session opens the system file picker — we cannot drive
        // the OS picker reliably via FlaUI, but the click itself must not crash
        // the shell. Send Escape immediately afterwards to dismiss any picker.
        shell.ToolbarButton("Open Session...").Click();
        System.Threading.Thread.Sleep(400);
        Keyboard.Type(VirtualKeyShort.ESCAPE);
        System.Threading.Thread.Sleep(200);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task ToolbarSaveSessionButton_Clicked_DoesNotCrash()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Save Session...").Click();
        System.Threading.Thread.Sleep(400);
        Keyboard.Type(VirtualKeyShort.ESCAPE);
        System.Threading.Thread.Sleep(200);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task NewTab_ThenCloseAllSecondaryTabs_LeavesDefaultTab()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Add two tabs.
        shell.NewTabButton().Click();
        System.Threading.Thread.Sleep(120);
        shell.NewTabButton().Click();
        shell.WaitUntil(
            () => shell.CloseTabButtons().Length == 2,
            description: "two close buttons visible after adding 2 tabs");

        // Close each secondary tab via its X button.
        while (shell.CloseTabButtons().Length > 0)
        {
            shell.CloseTabButtons()[0].Click();
            System.Threading.Thread.Sleep(120);
        }

        // The default first tab is sticky and remains.
        await Assert.That(shell.CloseTabButtons().Length).IsEqualTo(0);
    }
}
