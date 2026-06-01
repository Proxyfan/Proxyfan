using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the SSL Proxying tool window opened from
///     <c>Tools → SSL Proxying...</c> (<c>docs/DESIGN.md § 6.2 HTTPS
///     Decryption</c>).
/// </summary>
public sealed class ShellPageSecureSocketsLayerProxyingUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenSecureSocketsLayerProxying_FromToolsMenu_ShowsSslProxyingWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var ssl = shell.OpenToolWindow("Tools", "SSL Proxying...", "SSL Proxying");
        try
        {
            await Assert.That(ssl.GetTitle()).IsEqualTo("SSL Proxying");
            await Assert.That(ssl.CheckBox("Enable SSL proxying")).IsNotNull();
            await Assert.That(ssl.HasButton("Add")).IsTrue();
            await Assert.That(ssl.HasButton("Remove")).IsTrue();
        }
        finally
        {
            ssl.Close();
        }
    }

    [Test]
    public async Task OpenSecureSocketsLayerProxying_FreshWindow_HasNewPatternTextBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var ssl = shell.OpenToolWindow("Tools", "SSL Proxying...", "SSL Proxying");
        try
        {
            await Assert.That(ssl.TextBoxByName("New SSL pattern")).IsNotNull();
            await Assert.That(ssl.HasButton("Add")).IsTrue();
        }
        finally
        {
            ssl.Close();
        }
    }

    [Test]
    public async Task TypeNewPattern_FreshSecureSocketsLayerProxying_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var ssl = shell.OpenToolWindow("Tools", "SSL Proxying...", "SSL Proxying");
        try
        {
            var patternBox = ssl.TextBoxByName("New SSL pattern");
            patternBox.Focus();
            Keyboard.Type("*.example.com");
            ssl.WaitUntil(
                () => string.Equals(patternBox.Text, "*.example.com", StringComparison.Ordinal),
                description: "SSL pattern textbox populated");
        }
        finally
        {
            ssl.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task EnableSslProxyingCheckBox_ClickedTwice_TogglesAndRestores()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var ssl = shell.OpenToolWindow("Tools", "SSL Proxying...", "SSL Proxying");
        try
        {
            var enable = ssl.CheckBox("Enable SSL proxying");
            var initialState = enable.IsChecked == true;

            enable.Click();
            ssl.WaitUntil(
                () => (enable.IsChecked == true) != initialState,
                description: "SSL proxying toggled to opposite state");

            enable.Click();
            ssl.WaitUntil(
                () => (enable.IsChecked == true) == initialState,
                description: "SSL proxying toggled back to original state");
        }
        finally
        {
            ssl.Close();
        }

        await Task.CompletedTask;
    }
}
