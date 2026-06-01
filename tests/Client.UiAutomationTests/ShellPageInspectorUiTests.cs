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

    [Test]
    public async Task ClickHeadersTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var headersTab = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("Headers").And(cf.ByControlType(ControlType.TabItem)))
            ?? throw new System.InvalidOperationException("Headers tab not found.");
        headersTab.AsTabItem().Select();

        shell.WaitUntil(
            () => headersTab.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Headers tab reports IsSelected = true");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickBodyTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // There are two "Body" tabs (one each for Request and Response). Use
        // FindAllDescendants and pick the first one, then activate it.
        var bodyTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Body").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(bodyTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstBody = bodyTabs[0].AsTabItem();
        firstBody.Select();
        shell.WaitUntil(
            () => firstBody.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Body tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickRawTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var rawTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Raw").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(rawTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstRaw = rawTabs[0].AsTabItem();
        firstRaw.Select();
        shell.WaitUntil(
            () => firstRaw.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Raw tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickSummaryTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var summaryTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Summary").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(summaryTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstSummary = summaryTabs[0].AsTabItem();
        firstSummary.Select();
        shell.WaitUntil(
            () => firstSummary.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Summary tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickQueryTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var queryTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Query").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(queryTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstQuery = queryTabs[0].AsTabItem();
        firstQuery.Select();
        shell.WaitUntil(
            () => firstQuery.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Query tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickCookiesTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var cookiesTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Cookies").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(cookiesTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstCookies = cookiesTabs[0].AsTabItem();
        firstCookies.Select();
        shell.WaitUntil(
            () => firstCookies.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Cookies tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickAuthTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var authTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Auth").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(authTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstAuth = authTabs[0].AsTabItem();
        firstAuth.Select();
        shell.WaitUntil(
            () => firstAuth.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Auth tab reports IsSelected = true");
    }

    [Test]
    public async Task ClickTimingTab_FreshShell_MarksTheTabAsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var timingTabs = shell.Window.FindAllDescendants(cf =>
            cf.ByName("Timing").And(cf.ByControlType(ControlType.TabItem)));
        await Assert.That(timingTabs.Length).IsGreaterThanOrEqualTo(1);

        var firstTiming = timingTabs[0].AsTabItem();
        firstTiming.Select();
        shell.WaitUntil(
            () => firstTiming.Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "Timing tab reports IsSelected = true");
    }
}
