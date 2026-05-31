using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Breakpoint tool window opened from
///     <c>Tools → Breakpoint...</c> (<c>docs/DESIGN.md § 6.7 Breakpoints</c>).
/// </summary>
public sealed class ShellPageBreakpointUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenBreakpoint_FromToolsMenu_ShowsBreakpointWindowWithCoreControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var breakpoint = shell.OpenToolWindow("Tools", "Breakpoint...", "Breakpoint");
        try
        {
            await Assert.That(breakpoint.GetTitle()).IsEqualTo("Breakpoint");
            await Assert.That(breakpoint.CheckBox("Enabled")).IsNotNull();
            await Assert.That(breakpoint.ComboBoxByName("Breakpoint phases")).IsNotNull();
            await Assert.That(breakpoint.TextBoxByName("New pattern")).IsNotNull();
            await Assert.That(breakpoint.ListBoxByName("Pending breakpoint pauses")).IsNotNull();
        }
        finally
        {
            breakpoint.Close();
        }
    }

    [Test]
    public async Task OpenBreakpoint_FreshWindow_HasResumeAndAbortButtons()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var breakpoint = shell.OpenToolWindow("Tools", "Breakpoint...", "Breakpoint");
        try
        {
            await Assert.That(breakpoint.HasButton("Resume")).IsTrue();
            await Assert.That(breakpoint.HasButton("Abort")).IsTrue();
        }
        finally
        {
            breakpoint.Close();
        }
    }

    [Test]
    public async Task AddPatternButton_AfterTypingPattern_AppendsRowToList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var breakpoint = shell.OpenToolWindow("Tools", "Breakpoint...", "Breakpoint");
        try
        {
            var patternBox = breakpoint.TextBoxByName("New pattern");
            patternBox.Focus();
            Keyboard.Type("api.example.com/orders");
            breakpoint.WaitUntil(
                () => string.Equals(patternBox.Text, "api.example.com/orders", StringComparison.Ordinal),
                description: "pattern textbox populated");

            breakpoint.Button("Add").Click();

            var patternList = breakpoint.ListBoxByName("Configured patterns");
            breakpoint.WaitUntil(
                () => patternList.Items.Length >= 1,
                description: "pattern list grew to at least 1 entry");
        }
        finally
        {
            breakpoint.Close();
        }

        await Task.CompletedTask;
    }
}
