using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Plugin Manager tool window opened from
///     <c>View → Plugin Manager...</c>.
/// </summary>
public sealed class ShellPagePluginManagerUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenPluginManager_FromViewMenu_ShowsPluginManagerWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var plugins = shell.OpenToolWindow("View", "Plugin Manager...", "Plugin Manager");
        try
        {
            await Assert.That(plugins.GetTitle()).IsEqualTo("Plugin Manager");
            await Assert.That(plugins.HasButton("Refresh")).IsTrue();
            await Assert.That(plugins.HasButton("Reload")).IsTrue();
            await Assert.That(plugins.HasButton("Check for updates")).IsTrue();
        }
        finally
        {
            plugins.Close();
        }
    }

    [Test]
    public async Task RefreshButton_EmptyPluginManager_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var plugins = shell.OpenToolWindow("View", "Plugin Manager...", "Plugin Manager");
        try
        {
            plugins.Button("Refresh").Click();

            await Assert.That(plugins.GetTitle()).IsEqualTo("Plugin Manager");
            await Assert.That(plugins.HasButton("Refresh")).IsTrue();
        }
        finally
        {
            plugins.Close();
        }
    }
}
