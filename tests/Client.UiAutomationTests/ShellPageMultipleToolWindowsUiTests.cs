using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests that open multiple tool windows simultaneously
///     and assert the shell remains responsive with all of them on screen
///     (<c>docs/DESIGN.md § 4.5 Menu Bar</c>, <c>§ 4.8 Layout Options</c>).
///     Tool windows are independent top-level owned windows so the shell
///     must not lock or lose focus when several are open at once.
/// </summary>
public sealed class ShellPageMultipleToolWindowsUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenPreferencesAndBlockList_FromMenus_BothWindowsAreLive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var preferences = shell.OpenToolWindow("File", "Preferences...", "Preferences");
        try
        {
            using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
            try
            {
                await Assert.That(preferences.GetTitle()).IsEqualTo("Preferences");
                await Assert.That(blockList.GetTitle()).IsEqualTo("Block List");

                // Shell window must still be responsive and addressable while
                // both tool windows are open.
                await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
            }
            finally
            {
                blockList.Close();
            }
        }
        finally
        {
            preferences.Close();
        }
    }

    [Test]
    public async Task OpenThemeAndKeyboardShortcuts_FromViewMenu_BothWindowsAreLive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var theme = shell.OpenToolWindow("View", "Theme...", "Theme");
        try
        {
            using var shortcuts = shell.OpenToolWindow("View", "Keyboard Shortcuts...", "Keyboard Shortcuts");
            try
            {
                await Assert.That(theme.GetTitle()).IsEqualTo("Theme");
                await Assert.That(shortcuts.GetTitle()).IsEqualTo("Keyboard Shortcuts");
                await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
            }
            finally
            {
                shortcuts.Close();
            }
        }
        finally
        {
            theme.Close();
        }
    }

    [Test]
    public async Task OpenThreeTools_FromMenu_AllWindowsAreLive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var blockList = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            using var allowList = shell.OpenToolWindow("Tools", "Allow List...", "Allow List");
            try
            {
                using var mapLocal = shell.OpenToolWindow("Tools", "Map Local...", "Map Local");
                try
                {
                    await Assert.That(blockList.GetTitle()).IsEqualTo("Block List");
                    await Assert.That(allowList.GetTitle()).IsEqualTo("Allow List");
                    await Assert.That(mapLocal.GetTitle()).IsEqualTo("Map Local");
                    await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
                }
                finally
                {
                    mapLocal.Close();
                }
            }
            finally
            {
                allowList.Close();
            }
        }
        finally
        {
            blockList.Close();
        }
    }
}
