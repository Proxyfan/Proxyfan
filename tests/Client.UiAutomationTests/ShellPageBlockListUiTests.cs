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
///     End-to-end FlaUI tests for the Block List tool window opened from
///     <c>Tools → Block List...</c> (<c>docs/DESIGN.md § 6.10 Block List</c>).
/// </summary>
public sealed class ShellPageBlockListUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenBlockList_FromToolsMenu_ShowsBlockListWindowWithEnabledCheckBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            await Assert.That(blockList.GetTitle()).IsEqualTo("Block List");
            await Assert.That(blockList.CheckBox("Enabled")).IsNotNull();
            await Assert.That(blockList.HasButton("Add")).IsTrue();
        }
        finally
        {
            blockList.Close();
        }
    }

    [Test]
    public async Task OpenBlockList_FreshWindow_ExposesPatternInputAndPatternList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            await Assert.That(blockList.TextBoxByName("New pattern")).IsNotNull();
            await Assert.That(blockList.ComboBoxByName("Match kind")).IsNotNull();
            await Assert.That(blockList.ListBoxByName("Configured patterns")).IsNotNull();
        }
        finally
        {
            blockList.Close();
        }
    }

    [Test]
    public async Task AddPatternButton_AfterTypingPattern_AppendsRowToList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            var patternBox = blockList.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("evil.example.com");
            blockList.WaitUntil(
                () => string.Equals(patternBox.Text, "evil.example.com", StringComparison.Ordinal),
                description: "pattern textbox populated");

            blockList.Button("Add").Click();

            var patternList = blockList.ListBoxByName("Configured patterns");
            blockList.WaitUntil(
                () => patternList.Items.Length >= 1,
                description: "pattern list grew to at least 1 entry");
        }
        finally
        {
            blockList.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task EnabledCheckBox_ClickedTwice_TogglesOffThenBackOn()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            var enabled = blockList.CheckBox("Enabled");
            // The Block List defaults to enabled; toggle off, then back on.
            var initialState = enabled.IsChecked == true;
            enabled.Click();
            blockList.WaitUntil(
                () => (enabled.IsChecked == true) != initialState,
                description: "Enabled checkbox toggled to opposite state");

            enabled.Click();
            blockList.WaitUntil(
                () => (enabled.IsChecked == true) == initialState,
                description: "Enabled checkbox toggled back to original state");
        }
        finally
        {
            blockList.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddThenRemovePattern_FreshBlockList_LeavesListEmpty()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            var patternBox = blockList.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("tracker.example.com");
            blockList.WaitUntil(
                () => string.Equals(patternBox.Text, "tracker.example.com", StringComparison.Ordinal),
                description: "pattern textbox populated");
            blockList.Button("Add").Click();

            var list = blockList.ListBoxByName("Configured patterns");
            blockList.WaitUntil(() => list.Items.Length == 1, "list has 1 entry");

            // Each row renders a "Remove" button next to the pattern. Click
            // the first one to remove the entry we just added.
            var removeButton = blockList.Window.FindFirstDescendant(cf =>
                cf.ByName("Remove").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))
                ?? throw new InvalidOperationException("Remove button not found on added row.");
            removeButton.AsButton().Click();

            blockList.WaitUntil(
                () => list.Items.Length == 0,
                description: "pattern list returns to empty after removal");
        }
        finally
        {
            blockList.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddPatternButton_EmptyPatternText_LeavesListEmpty()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            // Clicking Add with no pattern text must be a safe no-op — the
            // command's CanExecute should block the add. The list stays empty.
            blockList.Button("Add").Click();
            System.Threading.Thread.Sleep(200);

            var list = blockList.ListBoxByName("Configured patterns");
            await Assert.That(list.Items.Length).IsEqualTo(0);
        }
        finally
        {
            blockList.Close();
        }
    }
}
