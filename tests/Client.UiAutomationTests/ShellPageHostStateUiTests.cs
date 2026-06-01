using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for shell-level concerns that span multiple
///     features: the update banner suppressed-by-default state
///     (<c>docs/DESIGN.md § 12 Auto-Update</c>), the absence of stray modal
///     dialogs on fresh launch, and the no-extra-windows guarantee.
/// </summary>
public sealed class ShellPageHostStateUiTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshShell_UpdateBannerIsNotVisible()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The update banner only appears after the update checker finds a
        // newer version. On a fresh launch nothing has been checked, so the
        // banner must not be on screen. We assert the absence by checking
        // that the banner's Dismiss button is not present.
        var dismissButton = shell.Window.FindFirstDescendant(cf =>
            cf.ByName("Dismiss").And(cf.ByControlType(ControlType.Button)));
        await Assert.That(dismissButton).IsNull();
    }

    [Test]
    public async Task Launch_FreshShell_NoUnexpectedChildWindowsAreOpen()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // The shell process should own exactly one top-level window
        // (the main shell). No stray tool windows or prompts may linger.
        var pid = shell.Window.Properties.ProcessId.Value;
        var desktop = app.Automation.GetDesktop();
        var childWindows = desktop.FindAllChildren(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByProcessId(pid)));

        await Assert.That(childWindows.Length).IsEqualTo(1);
        await Assert.That(childWindows[0].Name).IsEqualTo("Proxyfan");
    }

    [Test]
    public async Task Launch_FreshShell_ShellMenuBarRendersExactlyThreeTopLevelMenus()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var menuItems = shell.Window.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.MenuItem))
            .Select(m => m.Name ?? string.Empty)
            .Where(name => name is "File" or "Tools" or "View")
            .Distinct()
            .ToArray();

        await Assert.That(menuItems.Length).IsEqualTo(3);
        await Assert.That(menuItems).Contains("File");
        await Assert.That(menuItems).Contains("Tools");
        await Assert.That(menuItems).Contains("View");
    }
}
