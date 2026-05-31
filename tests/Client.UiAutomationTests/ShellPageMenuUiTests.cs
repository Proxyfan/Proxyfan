using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the shell <c>Menu</c> bar
///     (<c>docs/DESIGN.md § 4.5 Menu Bar</c>). Each test opens a top-level menu
///     via real mouse click, asserts that the expected sub-items are present,
///     and then closes the menu without invoking a command.
///     <para>
///         Invoking a menu command launches a tool window which would either
///         pop a modal dialog or open a sub-window — those flows are exercised
///         independently in dedicated tool-window tests.
///     </para>
/// </summary>
public sealed class ShellPageMenuUiTests : UiAutomationTestBase
{
    [Test]
    public async Task FileMenu_OpenedViaClick_ContainsPreferencesAndExitItems()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var fileMenu = OpenTopLevelMenu(shell, "File");

        try
        {
            var subItems = SubItemNames(fileMenu, shell);
            await Assert.That(subItems).Contains("Preferences...");
            await Assert.That(subItems).Contains("Exit");
        }
        finally
        {
            CloseMenu(shell);
        }
    }

    [Test]
    public async Task ToolsMenu_OpenedViaClick_ContainsExpectedToolItems()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var toolsMenu = OpenTopLevelMenu(shell, "Tools");

        try
        {
            var subItems = SubItemNames(toolsMenu, shell);
            await Assert.That(subItems).Contains("Block List...");
            await Assert.That(subItems).Contains("Allow List...");
            await Assert.That(subItems).Contains("Map Local...");
            await Assert.That(subItems).Contains("Map Remote...");
            await Assert.That(subItems).Contains("Throttle...");
        }
        finally
        {
            CloseMenu(shell);
        }
    }

    [Test]
    public async Task ViewMenu_OpenedViaClick_ContainsThemeKeyboardShortcutsAndPluginManager()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var viewMenu = OpenTopLevelMenu(shell, "View");

        try
        {
            var subItems = SubItemNames(viewMenu, shell);
            await Assert.That(subItems).Contains("Theme...");
            await Assert.That(subItems).Contains("Keyboard Shortcuts...");
            await Assert.That(subItems).Contains("Plugin Manager...");
        }
        finally
        {
            CloseMenu(shell);
        }
    }

    private static MenuItem OpenTopLevelMenu(ShellPage shell, string header)
    {
        var raw = shell.Window.FindFirstDescendant(cf =>
            cf.ByName(header).And(cf.ByControlType(ControlType.MenuItem)))
            ?? throw new InvalidOperationException($"Top-level menu '{header}' not present.");
        var typed = raw.AsMenuItem();

        // Try ExpandCollapse pattern first (the framework-supported way to
        // open a menu); fall back to mouse click + Enter if not supported.
        var expandCollapse = typed.Patterns.ExpandCollapse.PatternOrDefault;
        if (expandCollapse is not null)
        {
            expandCollapse.Expand();
        }
        else
        {
            typed.Click();
        }

        // Give the popup time to materialise.
        System.Threading.Thread.Sleep(300);
        shell.WaitUntil(
            () => DescendantMenuItems(shell, typed).Length > 0,
            description: $"submenu items to materialise under '{header}'");
        return typed;
    }

    private static string[] SubItemNames(MenuItem menuItem, ShellPage shell)
    {
        return DescendantMenuItems(shell, menuItem).Select(item => item.Name).ToArray();
    }

    private static MenuItem[] DescendantMenuItems(ShellPage shell, MenuItem topLevelMenuItem)
    {
        // Avalonia hosts opened submenus in a popup attached to the desktop
        // rather than as a descendant of the main window. Walk every MenuItem
        // attached to the same process across all top-levels.
        var topLevelName = topLevelMenuItem.Name;
        var processId = shell.Window.Properties.ProcessId.Value;
        var desktop = shell.Window.Automation.GetDesktop();
        var allItems = desktop.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByProcessId(processId)));
        return allItems
            .Where(item => !string.Equals(item.Name, topLevelName, StringComparison.Ordinal))
            .Select(item => item.AsMenuItem())
            .ToArray();
    }

    private static void CloseMenu(ShellPage shell)
    {
        // Press Escape on the window to dismiss any open menu so the shell is
        // ready for the next assertion / dispose.
        FlaUI.Core.Input.Keyboard.Type(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
        shell.Window.Focus();
    }
}
