using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests that exercise filter text persistence across
///     tab additions and toolbar interactions
///     (<c>docs/DESIGN.md § 6.4 Traffic Filtering</c>, § 6.25 Multiple Tabs).
///     The filter text box is a shell-level singleton bound to the active
///     tab's <c>TrafficListViewModel.FilterText</c>; the text it shows is
///     the active tab's filter.
/// </summary>
public sealed class ShellPageFilterPersistenceUiTests : UiAutomationTestBase
{
    [Test]
    public async Task TypedFilter_AfterAddingTab_FilterTextBoxReflectsActiveTabState()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("first-tab-filter");
        shell.WaitUntil(
            () => string.Equals(filter.Text, "first-tab-filter", StringComparison.Ordinal),
            "first-tab filter populated");

        // Adding a new tab switches the active tab; the filter textbox is now
        // bound to the new tab's filter (empty initially).
        shell.NewTabButton().Click();
        System.Threading.Thread.Sleep(300);

        var filterAfter = shell.FilterTextBox();
        shell.WaitUntil(
            () => string.IsNullOrEmpty(filterAfter.Text),
            "new-tab filter is empty after switch");

        await Task.CompletedTask;
    }

    [Test]
    public async Task TypedFilter_AfterClickingClearButton_FilterTextSurvives()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("survives-clear");
        shell.WaitUntil(
            () => string.Equals(filter.Text, "survives-clear", StringComparison.Ordinal),
            "filter populated");

        // Clear button on an empty flow list is a no-op against the FilterText
        // — the filter text must remain untouched.
        shell.ToolbarButton("Clear").Click();
        System.Threading.Thread.Sleep(150);

        await Assert.That(shell.FilterTextBox().Text).IsEqualTo("survives-clear");
    }
}
