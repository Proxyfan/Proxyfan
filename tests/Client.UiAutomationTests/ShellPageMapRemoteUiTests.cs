using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Map Remote tool window opened from
///     <c>Tools → Map Remote...</c> (<c>docs/DESIGN.md § 6.6 Map Remote</c>).
/// </summary>
public sealed class ShellPageMapRemoteUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenMapRemote_FromToolsMenu_ShowsMapRemoteWindowWithCoreControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapRemote = shell.OpenToolWindow("Tools", "Map Remote...", "Map Remote");
        try
        {
            await Assert.That(mapRemote.GetTitle()).IsEqualTo("Map Remote");
            await Assert.That(mapRemote.CheckBox("Enabled")).IsNotNull();
            await Assert.That(mapRemote.HasButton("Add")).IsTrue();
            await Assert.That(mapRemote.TextBoxByName("New pattern")).IsNotNull();
        }
        finally
        {
            mapRemote.Close();
        }
    }

    [Test]
    public async Task OpenMapRemote_FreshWindow_ExposesDestinationFieldsAndRuleList()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapRemote = shell.OpenToolWindow("Tools", "Map Remote...", "Map Remote");
        try
        {
            await Assert.That(mapRemote.TextBoxByName("Destination scheme")).IsNotNull();
            await Assert.That(mapRemote.TextBoxByName("Destination host")).IsNotNull();
            await Assert.That(mapRemote.TextBoxByName("Destination port")).IsNotNull();
            await Assert.That(mapRemote.TextBoxByName("Destination path")).IsNotNull();
            await Assert.That(mapRemote.ListBoxByName("Map Remote rules")).IsNotNull();
        }
        finally
        {
            mapRemote.Close();
        }
    }

    [Test]
    public async Task TypeDestinationFields_FreshWindow_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var mapRemote = shell.OpenToolWindow("Tools", "Map Remote...", "Map Remote");
        try
        {
            var schemeBox = mapRemote.TextBoxByName("Destination scheme");
            schemeBox.Focus();
            Keyboard.Type("https");
            mapRemote.WaitUntil(
                () => string.Equals(schemeBox.Text, "https", StringComparison.Ordinal),
                description: "scheme textbox populated");

            var hostBox = mapRemote.TextBoxByName("Destination host");
            hostBox.Focus();
            Keyboard.Type("staging.example.com");
            mapRemote.WaitUntil(
                () => string.Equals(hostBox.Text, "staging.example.com", StringComparison.Ordinal),
                description: "host textbox populated");
        }
        finally
        {
            mapRemote.Close();
        }

        await Task.CompletedTask;
    }
}
