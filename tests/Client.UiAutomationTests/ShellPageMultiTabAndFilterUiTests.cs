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
///     End-to-end FlaUI tests covering interactions across multiple tabs and
///     the filter text box, as described in <c>docs/DESIGN.md § 6.4 Traffic
///     Filtering</c> and § 6.25 Multiple Tabs. Each test drives the live UI
///     through real mouse and keyboard.
/// </summary>
public sealed class ShellPageMultiTabAndFilterUiTests : UiAutomationTestBase
{
    [Test]
    public async Task NewTabClickedThreeTimes_FreshShell_AddsThreeTabs()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        var newTab = shell.NewTabButton();
        var initial = shell.TabList().FindAllDescendants(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).Length;

        for (var i = 0; i < 3; i++)
        {
            newTab.Click();
            System.Threading.Thread.Sleep(100);
        }

        shell.WaitUntil(
            () =>
            {
                var count = shell.TabList().FindAllDescendants(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)).Length;
                return count == initial + 3;
            },
            description: $"tab strip to grow by 3 (from {initial} to {initial + 3})");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypedThenRetyped_ReplacesPreviousText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        var filter = shell.FilterTextBox();

        filter.Focus();
        Keyboard.Type("first");
        shell.WaitUntil(() => string.Equals(filter.Text, "first", StringComparison.Ordinal), "first typed");

        // Backspace to clear, then type again.
        for (var i = 0; i < "first".Length; i++)
        {
            Keyboard.Type(VirtualKeyShort.BACK);
        }
        shell.WaitUntil(() => string.IsNullOrEmpty(filter.Text), "cleared");

        Keyboard.Type("second");
        shell.WaitUntil(() => string.Equals(filter.Text, "second", StringComparison.Ordinal), "second typed");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypeMixedCase_PreservesExactCase()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        var filter = shell.FilterTextBox();

        filter.Focus();
        Keyboard.Type("MixedCASE.Example");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "MixedCASE.Example", StringComparison.Ordinal),
            description: "filter preserves mixed case exactly");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_FocusAfterClickingTabButton_FocusReturnsToFilter()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Click new tab (focus shifts to button), then click filter (focus shifts back), then type.
        shell.NewTabButton().Click();
        System.Threading.Thread.Sleep(150);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("after-tab");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "after-tab", StringComparison.Ordinal),
            description: "filter receives keystrokes after focus shift back from tab button");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClearButtonClickedThreeTimes_FreshShell_KeepsToolbarFunctional()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        for (var i = 0; i < 3; i++)
        {
            shell.ToolbarButton("Clear").Click();
            System.Threading.Thread.Sleep(100);
        }

        // After 3 idempotent clears the toolbar must still respond.
        await Assert.That(shell.ToolbarButton("Pause Capture").IsEnabled).IsTrue();
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }
}
