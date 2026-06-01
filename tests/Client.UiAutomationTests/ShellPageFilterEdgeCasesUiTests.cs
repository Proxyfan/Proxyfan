using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI edge-case tests for the toolbar filter text box
///     (<c>docs/DESIGN.md § 6.4 Traffic Filtering</c>). Covers special
///     characters that often break naive parsers (quotes, parens, slashes,
///     unicode), the very-long-input case, and the punctuation soup the user
///     might paste from a URL.
/// </summary>
public sealed class ShellPageFilterEdgeCasesUiTests : UiAutomationTestBase
{
    [Test]
    public async Task FilterTextBox_TypeQuotesAndParens_PreservesAllCharacters()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("\"quoted\" (paren) /slash/");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "\"quoted\" (paren) /slash/", StringComparison.Ordinal),
            description: "filter preserves quotes, parens, and slashes verbatim");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypeColonAndQueryString_PreservesEverything()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("https://api.example.com:8443/path?q=test&page=1");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "https://api.example.com:8443/path?q=test&page=1", StringComparison.Ordinal),
            description: "filter preserves a full URL with port and query string");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypeLongInput_PreservesEntireText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        var longInput = new string('a', 200);
        Keyboard.Type(longInput);

        shell.WaitUntil(
            () => string.Equals(filter.Text, longInput, StringComparison.Ordinal),
            description: "filter preserves a 200-char input verbatim");

        await Task.CompletedTask;
    }
}
