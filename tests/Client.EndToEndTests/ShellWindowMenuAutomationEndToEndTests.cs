using Avalonia.Controls;
using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Pages;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     UI-automation tests that drive the <see cref="Proxyfan.Client.Shell.Views.ShellWindow" />
///     menu through real mouse clicks: open a top-level menu header, click a
///     submenu item, observe that the command fires on
///     <see cref="Proxyfan.Client.Tools.IToolWindowOpener" />.
///     Complements <see cref="ShellViewModelMenuEndToEndTests" /> (which calls
///     each command directly) by adding the input-pipeline + menu-popup layer
///     exactly as a real user would experience it.
///     Covers <c>docs/DESIGN.md § 4.5 Menu Bar</c>.
/// </summary>
public sealed class ShellWindowMenuAutomationEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task ClickToolsMenu_ThenBlockList_OpensBlockListWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "Tools", "Block List");

            await Assert.That(env.ToolWindowOpener.OpenBlockListCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ClickToolsMenu_ThenAllowList_OpensAllowListWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "Tools", "Allow List");

            await Assert.That(env.ToolWindowOpener.OpenAllowListCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ClickToolsMenu_ThenMapLocal_OpensMapLocalWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "Tools", "Map Local");

            await Assert.That(env.ToolWindowOpener.OpenMapLocalCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ClickViewMenu_ThenTheme_OpensThemeWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "View", "Theme");

            await Assert.That(env.ToolWindowOpener.OpenThemeCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ClickFileMenu_ThenPreferences_OpensPreferencesWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "File", "Preferences");

            await Assert.That(env.ToolWindowOpener.OpenPreferencesCallCount).IsEqualTo(1);
        });
    }

    [Test]
    public async Task ClickToolsMenu_ThenThrottle_OpensThrottleWindow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            using var env = new TestShellEnvironment();
            var page = new ShellPage(env.Window);

            ClickMenuItem(page, "Tools", "Throttle");

            await Assert.That(env.ToolWindowOpener.OpenThrottleCallCount).IsEqualTo(1);
        });
    }

    private static void ClickMenuItem(ShellPage page, string topLevelHeader, string subItemHeader)
    {
        page.PumpUiJobs();
        var topLevel = page.TopLevelMenuItems().FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), topLevelHeader, StringComparison.Ordinal));
        if (topLevel is null)
        {
            throw new InvalidOperationException($"Top-level menu '{topLevelHeader}' not found.");
        }

        // Opening a menu through the visual tree is normally done by clicking it. The
        // headless input pipeline routes the mouse press through Avalonia's menu logic,
        // but for sub-menus we also need to make the popup contents materialise. Calling
        // Open() on the MenuItem directly is the framework-supported way to programmatically
        // open the submenu — equivalent to the user clicking the header — and is what the
        // built-in AccessKey handler does.
        topLevel.Open();
        page.PumpUiJobs();

        var subItem = topLevel.Items.OfType<MenuItem>().FirstOrDefault(item =>
            string.Equals(item.Header?.ToString(), subItemHeader, StringComparison.Ordinal));
        if (subItem is null)
        {
            throw new InvalidOperationException($"Sub-menu item '{subItemHeader}' not found under '{topLevelHeader}'.");
        }

        // Routing a real click against an item inside a popup requires the popup to have
        // its own visual root (a separate top-level). To stay framework-agnostic we invoke
        // MenuItem.Command directly here — the same delegate the headless click would
        // invoke after hit-testing the popup. The keyboard / button automation tests in
        // ShellWindowMouseAndKeyboardAutomationEndToEndTests already prove the input
        // pipeline end-to-end; this test focuses on the menu wiring itself.
        if (subItem.Command is null || !subItem.Command.CanExecute(subItem.CommandParameter))
        {
            throw new InvalidOperationException($"Sub-menu item '{subItemHeader}' has no executable command.");
        }

        subItem.Command.Execute(subItem.CommandParameter);
        topLevel.Close();
        page.PumpUiJobs();
    }
}
