using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Theme tool window opened from
///     <c>View → Theme...</c> (<c>docs/DESIGN.md § 8 Theming and Appearance</c>).
/// </summary>
public sealed class ShellPageThemeUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenTheme_FromViewMenu_ShowsThemeWindowWithPickerAndApply()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var theme = shell.OpenToolWindow("View", "Theme...", "Theme");
        try
        {
            await Assert.That(theme.GetTitle()).IsEqualTo("Theme");
            await Assert.That(theme.ListBoxByName("Theme list")).IsNotNull();
            await Assert.That(theme.HasButton("Apply")).IsTrue();
        }
        finally
        {
            theme.Close();
        }
    }

    [Test]
    public async Task OpenTheme_FreshWindow_PickerHasMultipleOptions()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var theme = shell.OpenToolWindow("View", "Theme...", "Theme");
        try
        {
            var options = theme.ListBoxByName("Theme list");
            theme.WaitUntil(
                () => options.Items.Length >= 2,
                description: "theme picker has at least two options (System, Light, Dark)");
        }
        finally
        {
            theme.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task SelectFirstTheme_FreshWindow_MarksTheItemAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var theme = shell.OpenToolWindow("View", "Theme...", "Theme");
        try
        {
            var options = theme.ListBoxByName("Theme list");
            theme.WaitUntil(
                () => options.Items.Length >= 1,
                description: "theme options populated");

            // Avalonia's ListBox UIA peer does not expose the Selection
            // pattern on every framework build; assert selection via the
            // ListItem's SelectionItemPattern.IsSelected.
            var firstItem = options.Items[0];
            firstItem.Select();
            theme.WaitUntil(
                () => firstItem.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
                description: "first theme item reports IsSelected = true");
        }
        finally
        {
            theme.Close();
        }

        await Task.CompletedTask;
    }
}
