using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Network Throttle tool window opened
///     from <c>Tools → Throttle...</c> (<c>docs/DESIGN.md § 6.12 Network
///     Throttling</c>).
/// </summary>
public sealed class ShellPageThrottleUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenThrottle_FromToolsMenu_ShowsNetworkThrottleWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var throttle = shell.OpenToolWindow("Tools", "Throttle...", "Network Throttle");
        try
        {
            await Assert.That(throttle.GetTitle()).IsEqualTo("Network Throttle");
            await Assert.That(throttle.HasButton("Apply")).IsTrue();
            await Assert.That(throttle.ListBoxByName("Throttle presets")).IsNotNull();
        }
        finally
        {
            throttle.Close();
        }
    }

    [Test]
    public async Task OpenThrottle_FreshWindow_PresetListContainsAtLeastOnePreset()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var throttle = shell.OpenToolWindow("Tools", "Throttle...", "Network Throttle");
        try
        {
            var presets = throttle.ListBoxByName("Throttle presets");
            throttle.WaitUntil(
                () => presets.Items.Length >= 1,
                description: "throttle presets list has at least one entry");
        }
        finally
        {
            throttle.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task SelectFirstPreset_FreshWindow_DoesNotCrashWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var throttle = shell.OpenToolWindow("Tools", "Throttle...", "Network Throttle");
        try
        {
            var presets = throttle.ListBoxByName("Throttle presets");
            throttle.WaitUntil(
                () => presets.Items.Length >= 1,
                description: "presets populated");

            // Avalonia's ListBox UIA peer does not expose the Selection
            // pattern on every framework build, so we cannot read
            // SelectedItems. We assert the click + selection round-trip via
            // the ListItem's SelectionItemPattern.IsSelected instead.
            var firstItem = presets.Items[0];
            firstItem.Select();

            throttle.WaitUntil(
                () => firstItem.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
                description: "first preset item reports IsSelected = true");
        }
        finally
        {
            throttle.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task SelectPresetAndApply_FreshWindow_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var throttle = shell.OpenToolWindow("Tools", "Throttle...", "Network Throttle");
        try
        {
            var presets = throttle.ListBoxByName("Throttle presets");
            throttle.WaitUntil(
                () => presets.Items.Length >= 1,
                description: "presets populated");

            presets.Items[0].Select();
            throttle.Button("Apply").Click();

            // The window must remain responsive after Apply.
            await Assert.That(throttle.GetTitle()).IsEqualTo("Network Throttle");
            await Assert.That(throttle.HasButton("Apply")).IsTrue();
        }
        finally
        {
            throttle.Close();
        }
    }
}
