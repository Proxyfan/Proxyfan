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
}
