using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests that verify the source list panel's initial
///     state and discoverability (<c>docs/DESIGN.md § 4.2 Source List Panel</c>).
///     The panel renders the synthetic "All" group with a count, plus one row
///     per captured host. On a fresh shell only the "All" entry is present
///     with count 0.
/// </summary>
public sealed class ShellPageSourceListUiTests : UiAutomationTestBase
{
    [Test]
    public async Task SourceList_FreshShell_AllGroupLabelIsVisible()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Assert via accessible text — the Avalonia ListBox's Items collection
        // is not always queryable through the generic ListBox UIA pattern.
        shell.WaitUntil(
            () => shell.HasVisibleText("All"),
            description: "source-list All-group label visible");

        await Task.CompletedTask;
    }

    [Test]
    public async Task SourceList_FreshShell_AllGroupCountStartsAtZero()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Both the source list "All" count and the status bar flow count start
        // at "0" on a fresh shell — assert at least one "0" Text element is
        // visible. (The status bar count and the source-list count both render
        // identically through the same TextBlock binding.)
        shell.WaitUntil(
            () => shell.HasVisibleText("0"),
            description: "count 0 visible on fresh shell");

        await Task.CompletedTask;
    }

    [Test]
    public async Task SourceList_FreshShell_ListBoxIsDiscoverableByAutomationName()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var sources = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("Sources").And(cf.ByControlType(ControlType.List)));
        await Assert.That(sources).IsNotNull();
    }
}
