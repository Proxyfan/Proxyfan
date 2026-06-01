using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Scripting tool window opened from
///     <c>Tools → Scripting...</c> (<c>docs/DESIGN.md § 6.8 Scripting (C#)</c>).
/// </summary>
public sealed class ShellPageScriptingUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenScripting_FromToolsMenu_ShowsScriptingWindowWithCompileButton()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var scripting = shell.OpenToolWindow("Tools", "Scripting...", "Scripting");
        try
        {
            await Assert.That(scripting.GetTitle()).IsEqualTo("Scripting");
            await Assert.That(scripting.CheckBox("Enabled")).IsNotNull();
            await Assert.That(scripting.HasButton("Compile and activate")).IsTrue();
            await Assert.That(scripting.HasButton("Clear active script")).IsTrue();
        }
        finally
        {
            scripting.Close();
        }
    }

    [Test]
    public async Task OpenScripting_FreshWindow_ExposesRequestAndResponseScriptBoxes()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var scripting = shell.OpenToolWindow("Tools", "Scripting...", "Scripting");
        try
        {
            await Assert.That(scripting.TextBoxByName("OnRequest C# script")).IsNotNull();
            await Assert.That(scripting.TextBoxByName("OnResponse C# script")).IsNotNull();
        }
        finally
        {
            scripting.Close();
        }
    }

    [Test]
    public async Task TypeIntoRequestScript_FreshScripting_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var scripting = shell.OpenToolWindow("Tools", "Scripting...", "Scripting");
        try
        {
            var requestBox = scripting.TextBoxByName("OnRequest C# script");
            requestBox.Focus();
            Keyboard.Type("// hello");
            scripting.WaitUntil(
                () => requestBox.Text.Contains("// hello", StringComparison.Ordinal),
                description: "request script box contains typed comment");
        }
        finally
        {
            scripting.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task EnabledCheckBox_ClickedTwice_TogglesAndRestores()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var scripting = shell.OpenToolWindow("Tools", "Scripting...", "Scripting");
        try
        {
            var enabled = scripting.CheckBox("Enabled");
            var initialState = enabled.IsChecked == true;

            enabled.Click();
            scripting.WaitUntil(
                () => (enabled.IsChecked == true) != initialState,
                description: "Scripting Enabled toggled to opposite state");

            enabled.Click();
            scripting.WaitUntil(
                () => (enabled.IsChecked == true) == initialState,
                description: "Scripting Enabled toggled back to original state");
        }
        finally
        {
            scripting.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickClearActiveScript_EmptyScripting_DoesNotCrashWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var scripting = shell.OpenToolWindow("Tools", "Scripting...", "Scripting");
        try
        {
            scripting.Button("Clear active script").Click();

            // The button is enabled at all times; with no script loaded the
            // click must be a safe no-op.
            await Assert.That(scripting.GetTitle()).IsEqualTo("Scripting");
        }
        finally
        {
            scripting.Close();
        }
    }
}
