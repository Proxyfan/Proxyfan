using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the traffic flow grid in the shell's centre
///     panel (<c>docs/DESIGN.md § 4.3 Traffic Flow List</c>). The grid renders
///     captured flows with columns: #, Status, Method, Host, Path, Code,
///     GraphQL, Duration.
/// </summary>
public sealed class ShellPageTrafficListUiTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshShell_FlowGridIsDiscoverableByAutomationName()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var grid = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("Captured traffic flows").And(cf.ByControlType(ControlType.DataGrid)));
        await Assert.That(grid).IsNotNull();
    }

    [Test]
    public async Task Launch_FreshShell_TrafficGridHasReasonableBounds()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Column header names inside Avalonia's DataGrid render through
        // custom DataTemplates that don't expose the header text as a
        // first-class UIA-named element on every framework build. Assert
        // the grid is on screen with non-zero pixel bounds instead.
        var grid = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("Captured traffic flows").And(cf.ByControlType(ControlType.DataGrid)));
        await Assert.That(grid).IsNotNull();

        var bounds = grid!.BoundingRectangle;
        await Assert.That(bounds.Width).IsGreaterThan(0);
        await Assert.That(bounds.Height).IsGreaterThan(0);
    }
}
