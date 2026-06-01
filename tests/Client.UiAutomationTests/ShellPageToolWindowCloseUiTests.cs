using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests that verify tool windows close cleanly when
///     their owning ToolWindowPage is disposed (<c>docs/DESIGN.md § 4.5
///     Menu Bar</c>). Closing must not leak any child window owned by the
///     shell process, and the shell window itself must remain responsive.
/// </summary>
public sealed class ShellPageToolWindowCloseUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenAndClosePreferences_FreshShell_NoLingeringChildWindowRemains()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using (var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences"))
        {
            await Assert.That(preferences.GetTitle()).IsEqualTo("Preferences");
            preferences.Close();
        }

        // After dispose only the main shell window may remain as a child of
        // the desktop owned by this process.
        System.Threading.Thread.Sleep(400);
        var pid = shell.Window.Properties.ProcessId.Value;
        var children = app.Automation.GetDesktop().FindAllChildren(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByProcessId(pid)));
        var names = children.Select(c => c.Name ?? string.Empty).ToArray();
        await Assert.That(names.Length).IsEqualTo(1);
        await Assert.That(names[0]).IsEqualTo("Proxyfan");
    }

    [Test]
    public async Task OpenAndCloseBlockList_FreshShell_NoLingeringChildWindowRemains()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using (var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List"))
        {
            await Assert.That(blockList.GetTitle()).IsEqualTo("Block List");
            blockList.Close();
        }

        System.Threading.Thread.Sleep(400);
        var pid = shell.Window.Properties.ProcessId.Value;
        var children = app.Automation.GetDesktop().FindAllChildren(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByProcessId(pid)));
        var names = children.Select(c => c.Name ?? string.Empty).ToArray();
        await Assert.That(names.Length).IsEqualTo(1);
        await Assert.That(names[0]).IsEqualTo("Proxyfan");
    }

    [Test]
    public async Task OpenAndCloseTheme_FreshShell_NoLingeringChildWindowRemains()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using (var theme = shell.OpenToolWindow("View", "Theme...", "Theme"))
        {
            await Assert.That(theme.GetTitle()).IsEqualTo("Theme");
            theme.Close();
        }

        System.Threading.Thread.Sleep(400);
        var pid = shell.Window.Properties.ProcessId.Value;
        var children = app.Automation.GetDesktop().FindAllChildren(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByProcessId(pid)));
        var names = children.Select(c => c.Name ?? string.Empty).ToArray();
        await Assert.That(names.Length).IsEqualTo(1);
        await Assert.That(names[0]).IsEqualTo("Proxyfan");
    }
}
