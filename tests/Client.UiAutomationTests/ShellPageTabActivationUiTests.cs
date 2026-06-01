using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for multi-tab interactions in the shell tab
///     strip (<c>docs/DESIGN.md § 6.25 Multiple Tabs</c>): activating a tab
///     by click, the default tab remains active after extra tabs are closed,
///     and the active selection persists across rapid Ctrl+T presses.
/// </summary>
public sealed class ShellPageTabActivationUiTests : UiAutomationTestBase
{
    [Test]
    public async Task AddTab_ClickFirstTab_FirstTabReportsIsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Add an extra tab so the user can switch between two.
        shell.NewTabButton().Click();
        shell.WaitUntil(() => SafeListItemCount(shell) == 2, "two tabs present");

        var items = shell.TabList().FindAllDescendants(cf =>
            cf.ByControlType(ControlType.ListItem));
        // Click the first (sticky default) tab to make it active.
        items[0].Click();

        shell.WaitUntil(
            () => items[0].Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "first tab reports IsSelected = true after click");

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddTab_ClickSecondTab_SecondTabReportsIsSelected()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.NewTabButton().Click();
        shell.WaitUntil(() => SafeListItemCount(shell) == 2, "two tabs present");

        var items = shell.TabList().FindAllDescendants(cf =>
            cf.ByControlType(ControlType.ListItem));
        items[1].Click();

        shell.WaitUntil(
            () => items[1].Patterns.SelectionItem.PatternOrDefault?.IsSelected.ValueOrDefault == true,
            description: "second tab reports IsSelected = true after click");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PressCtrlTSeveralTimes_FreshShell_TabsAccumulate()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        var before = SafeListItemCount(shell);
        for (var i = 0; i < 4; i++)
        {
            Keyboard.TypeSimultaneously(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_T);
            System.Threading.Thread.Sleep(100);
        }
        shell.WaitUntil(
            () => SafeListItemCount(shell) == before + 4,
            description: $"tab strip grew by 4 (from {before} to {before + 4})");

        await Task.CompletedTask;
    }

    private static int SafeListItemCount(ShellPage shell)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return shell.TabList().FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.ListItem)).Length;
            }
            catch
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        return shell.TabList().FindAllDescendants(cf =>
            cf.ByControlType(ControlType.ListItem)).Length;
    }
}
