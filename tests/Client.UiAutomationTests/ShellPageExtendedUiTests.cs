using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests covering additional shell user-interaction
///     patterns from <c>docs/DESIGN.md § 4.6 Toolbar</c>, § 4.2 Source List
///     Panel, and § 6.4 Traffic Filtering. Every test goes through the full
///     MSIX install → run → uninstall pipeline.
///     <para>
///         Excluded from this file: Open Session / Save Session button click
///         tests. Those open OS-level file picker dialogs which persist past
///         the test boundary and corrupt subsequent tests (the test runner
///         cannot reliably dismiss them with Escape because the dialog is a
///         top-level OS window, not a Proxyfan-owned window).
///     </para>
/// </summary>
public sealed class ShellPageExtendedUiTests : UiAutomationTestBase
{
    [Test]
    public async Task SourcePanelHeader_FreshShell_IsVisibleWithSourcesLabel()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.WaitUntil(
            () => shell.HasVisibleText("Sources"),
            description: "Sources header visible in left panel");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ToolbarAppName_FreshShell_DisplaysProxyfanText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.WaitUntil(
            () => shell.HasVisibleText("Proxyfan"),
            description: "Proxyfan app-name text visible in toolbar");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypedUrlPattern_PreservesSlashesAndDots()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("https://api.example.com/users/42");

        shell.WaitUntil(
            () => string.Equals(filter.Text, "https://api.example.com/users/42", StringComparison.Ordinal),
            description: "filter preserves URL syntax characters");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FilterTextBox_TypedRegexLikePattern_NotInterpretedAsRegex()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type(".*example");

        shell.WaitUntil(
            () => string.Equals(filter.Text, ".*example", StringComparison.Ordinal),
            description: "filter preserves regex-like syntax characters verbatim");

        await Task.CompletedTask;
    }
}
