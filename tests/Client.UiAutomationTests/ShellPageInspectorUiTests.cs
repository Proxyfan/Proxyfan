using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the inspector pane (right-hand side of the
///     shell), covering <c>docs/DESIGN.md § 4.4 Inspector Panel</c> and § 6.3
///     Traffic Inspection. The inspector hosts request and response sub-panels,
///     each with a fixed set of tabs (Headers, Body, Query, Cookies, Auth,
///     Raw, Summary, Timing, GraphQL).
/// </summary>
public sealed class ShellPageInspectorUiTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshShell_InspectorTabControlsArePresent()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The inspector pane contains two TabControls (Request and Response
        // sections). Each TabControl is discoverable; the section headers
        // themselves are HeaderedContentControl headers which may or may not
        // surface as named UIA elements depending on framework build.
        var tabControls = shell.Window.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Tab));
        await Assert.That(tabControls.Length).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Launch_FreshShell_RequestAndResponseTabsAreDiscoverable()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var tabNames = shell.Window.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.TabItem))
            .Select(t => t.Name ?? string.Empty)
            .Distinct()
            .ToArray();

        // The inspector hosts a fixed set of tabs in both Request and Response
        // sections. We expect at minimum: Headers, Body, Raw, Summary. The
        // exact tab set depends on flow type, but those four are always
        // present on a fresh launch.
        await Assert.That(tabNames).Contains("Headers");
        await Assert.That(tabNames).Contains("Body");
        await Assert.That(tabNames).Contains("Raw");
        await Assert.That(tabNames).Contains("Summary");
    }

    [Test]
    public async Task Launch_FreshShell_AdditionalInspectorTabsArePresent()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var tabNames = shell.Window.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.TabItem))
            .Select(t => t.Name ?? string.Empty)
            .Distinct()
            .ToArray();

        await Assert.That(tabNames).Contains("Query");
        await Assert.That(tabNames).Contains("Cookies");
        await Assert.That(tabNames).Contains("Auth");
        await Assert.That(tabNames).Contains("Timing");
    }
}
