using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Request Composer tool window opened
///     from <c>Tools → Compose Request...</c> (<c>docs/DESIGN.md § 6.15
///     Repeat Request</c>).
/// </summary>
public sealed class ShellPageComposerUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenComposer_FromToolsMenu_ShowsRequestComposerWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var composer = shell.OpenToolWindow("Tools", "Compose Request...", "Request Composer");
        try
        {
            await Assert.That(composer.GetTitle()).IsEqualTo("Request Composer");
            await Assert.That(composer.HasButton("Send")).IsTrue();
            await Assert.That(composer.HasButton("Copy as cURL")).IsTrue();
        }
        finally
        {
            composer.Close();
        }
    }

    [Test]
    public async Task OpenComposer_FreshWindow_ExposesMethodAndUrlAndBodyControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var composer = shell.OpenToolWindow("Tools", "Compose Request...", "Request Composer");
        try
        {
            await Assert.That(composer.ComboBoxByName("HTTP method")).IsNotNull();
            await Assert.That(composer.TextBoxByName("Request URL")).IsNotNull();
            await Assert.That(composer.TextBoxByName("Request headers")).IsNotNull();
            await Assert.That(composer.TextBoxByName("Request body")).IsNotNull();
        }
        finally
        {
            composer.Close();
        }
    }

    [Test]
    public async Task TypeIntoUrl_FreshComposer_PreservesTypedText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var composer = shell.OpenToolWindow("Tools", "Compose Request...", "Request Composer");
        try
        {
            var url = composer.TextBoxByName("Request URL");
            url.Focus();
            Keyboard.Type("https://example.com/api/v1/health");
            composer.WaitUntil(
                () => string.Equals(url.Text, "https://example.com/api/v1/health", StringComparison.Ordinal),
                description: "URL textbox populated");
        }
        finally
        {
            composer.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task TypeIntoBody_FreshComposer_PreservesTypedJson()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var composer = shell.OpenToolWindow("Tools", "Compose Request...", "Request Composer");
        try
        {
            var body = composer.TextBoxByName("Request body");
            body.Focus();
            Keyboard.Type("{\"hello\":\"world\"}");
            composer.WaitUntil(
                () => body.Text.Contains("{\"hello\":\"world\"}", StringComparison.Ordinal),
                description: "body textbox contains typed JSON");
        }
        finally
        {
            composer.Close();
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task TypeIntoHeaders_FreshComposer_PreservesTypedHeader()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var composer = shell.OpenToolWindow("Tools", "Compose Request...", "Request Composer");
        try
        {
            var headers = composer.TextBoxByName("Request headers");
            headers.Focus();
            Keyboard.Type("X-Test-Header: value");
            composer.WaitUntil(
                () => headers.Text.Contains("X-Test-Header: value", StringComparison.Ordinal),
                description: "headers textbox contains typed header");
        }
        finally
        {
            composer.Close();
        }

        await Task.CompletedTask;
    }
}
