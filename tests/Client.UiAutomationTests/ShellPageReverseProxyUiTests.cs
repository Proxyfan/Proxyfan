using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Reverse Proxy tool window opened from
///     <c>Tools → Reverse Proxy...</c>.
/// </summary>
public sealed class ShellPageReverseProxyUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenReverseProxy_FromToolsMenu_ShowsReverseProxyWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var reverse = shell.OpenToolWindow("Tools", "Reverse Proxy...", "Reverse Proxy");
        try
        {
            await Assert.That(reverse.GetTitle()).IsEqualTo("Reverse Proxy");
            await Assert.That(reverse.HasButton("Add")).IsTrue();
            await Assert.That(reverse.ListBoxByName("Reverse proxy routes")).IsNotNull();
        }
        finally
        {
            reverse.Close();
        }
    }

    [Test]
    public async Task OpenReverseProxy_FreshWindow_ExposesRouteFormFields()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var reverse = shell.OpenToolWindow("Tools", "Reverse Proxy...", "Reverse Proxy");
        try
        {
            await Assert.That(reverse.TextBoxByName("Route name")).IsNotNull();
            await Assert.That(reverse.TextBoxByName("Listen port")).IsNotNull();
            await Assert.That(reverse.TextBoxByName("Backend host")).IsNotNull();
            await Assert.That(reverse.TextBoxByName("Backend port")).IsNotNull();
            await Assert.That(reverse.ComboBoxByName("TLS mode")).IsNotNull();
        }
        finally
        {
            reverse.Close();
        }
    }

    [Test]
    public async Task TypeRouteName_FreshReverseProxy_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var reverse = shell.OpenToolWindow("Tools", "Reverse Proxy...", "Reverse Proxy");
        try
        {
            var routeNameBox = reverse.TextBoxByName("Route name");
            routeNameBox.Focus();
            Keyboard.Type("dev-api");
            reverse.WaitUntil(
                () => string.Equals(routeNameBox.Text, "dev-api", StringComparison.Ordinal),
                description: "route name textbox populated");
        }
        finally
        {
            reverse.Close();
        }

        await Task.CompletedTask;
    }
}
