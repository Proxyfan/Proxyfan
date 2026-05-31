using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Map Local tool window opened from
///     <c>Tools → Map Local...</c> (<c>docs/DESIGN.md § 6.5 Map Local</c>).
/// </summary>
public sealed class ShellPageMapLocalUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenMapLocal_FromToolsMenu_ShowsMapLocalWindowWithCoreControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapLocal = shell.OpenToolWindow("Tools", "Map Local...", "Map Local");
        try
        {
            await Assert.That(mapLocal.GetTitle()).IsEqualTo("Map Local");
            await Assert.That(mapLocal.CheckBox("Enabled")).IsNotNull();
            await Assert.That(mapLocal.HasButton("Add")).IsTrue();
            await Assert.That(mapLocal.TextBoxByName("New pattern")).IsNotNull();
        }
        finally
        {
            mapLocal.Close();
        }
    }

    [Test]
    public async Task OpenMapLocal_FreshWindow_ExposesResponseFieldsAndRuleList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapLocal = shell.OpenToolWindow("Tools", "Map Local...", "Map Local");
        try
        {
            await Assert.That(mapLocal.TextBoxByName("Local response status code")).IsNotNull();
            await Assert.That(mapLocal.TextBoxByName("Local response reason phrase")).IsNotNull();
            await Assert.That(mapLocal.TextBoxByName("Local response headers")).IsNotNull();
            await Assert.That(mapLocal.TextBoxByName("Local response body")).IsNotNull();
            await Assert.That(mapLocal.ListBoxByName("Map Local rules")).IsNotNull();
        }
        finally
        {
            mapLocal.Close();
        }
    }

    [Test]
    public async Task AddEntry_AfterTypingPattern_AppendsRowToRuleList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapLocal = shell.OpenToolWindow("Tools", "Map Local...", "Map Local");
        try
        {
            var patternBox = mapLocal.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("https://api.local/users");
            mapLocal.WaitUntil(
                () => string.Equals(patternBox.Text, "https://api.local/users", StringComparison.Ordinal),
                description: "pattern textbox populated");

            mapLocal.Button("Add").Click();

            var ruleList = mapLocal.ListBoxByName("Map Local rules");
            mapLocal.WaitUntil(
                () => ruleList.Items.Length >= 1,
                description: "Map Local rule list grew to at least 1 entry");
        }
        finally
        {
            mapLocal.Close();
        }

        await Task.CompletedTask;
    }
}
