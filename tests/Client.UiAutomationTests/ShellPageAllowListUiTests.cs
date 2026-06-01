using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Allow List tool window opened from
///     <c>Tools → Allow List...</c> (<c>docs/DESIGN.md § 6.9 Allow List</c>).
/// </summary>
public sealed class ShellPageAllowListUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenAllowList_FromToolsMenu_ShowsAllowListWindowWithEnabledCheckBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var allowList = shell.OpenToolWindow("Tools", "Allow List...", "Allow List");
        try
        {
            await Assert.That(allowList.GetTitle()).IsEqualTo("Allow List");
            await Assert.That(allowList.CheckBox("Enabled")).IsNotNull();
            await Assert.That(allowList.HasButton("Add")).IsTrue();
        }
        finally
        {
            allowList.Close();
        }
    }

    [Test]
    public async Task OpenAllowList_FreshWindow_ExposesPatternInputAndPatternList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var allowList = shell.OpenToolWindow("Tools", "Allow List...", "Allow List");
        try
        {
            await Assert.That(allowList.TextBoxByName("New pattern")).IsNotNull();
            await Assert.That(allowList.ComboBoxByName("Match kind")).IsNotNull();
            await Assert.That(allowList.ListBoxByName("Configured patterns")).IsNotNull();
        }
        finally
        {
            allowList.Close();
        }
    }

    [Test]
    public async Task AddPatternButton_AfterTypingPattern_AppendsRowToList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var allowList = shell.OpenToolWindow("Tools", "Allow List...", "Allow List");
        try
        {
            var patternBox = allowList.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("trusted.example.com");
            allowList.WaitUntil(
                () => string.Equals(patternBox.Text, "trusted.example.com", StringComparison.Ordinal),
                description: "pattern textbox populated");

            allowList.Button("Add").Click();

            var patternList = allowList.ListBoxByName("Configured patterns");
            allowList.WaitUntil(
                () => patternList.Items.Length >= 1,
                description: "pattern list grew to at least 1 entry");
        }
        finally
        {
            allowList.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddThenRemovePattern_FreshAllowList_LeavesListEmpty()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var allowList = shell.OpenToolWindow("Tools", "Allow List...", "Allow List");
        try
        {
            var patternBox = allowList.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("api.allowed.com");
            allowList.WaitUntil(
                () => string.Equals(patternBox.Text, "api.allowed.com", StringComparison.Ordinal),
                description: "pattern textbox populated");
            allowList.Button("Add").Click();

            var list = allowList.ListBoxByName("Configured patterns");
            allowList.WaitUntil(() => list.Items.Length == 1, "list has 1 entry");

            var removeButton = allowList.Window.FindFirstDescendant(cf =>
                cf.ByName("Remove").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))
                ?? throw new InvalidOperationException("Remove button not found on added row.");
            removeButton.AsButton().Click();

            allowList.WaitUntil(
                () => list.Items.Length == 0,
                description: "pattern list returns to empty after removal");
        }
        finally
        {
            allowList.Close();
        }

        await Task.CompletedTask;
    }
}
