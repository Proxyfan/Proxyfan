using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests that exercise tool window re-open behaviour:
///     each <see cref="Proxyfan.Client.Tools.AvaloniaToolWindowOpener" /> tool
///     window is singleton-per-process. Re-opening the same menu item should
///     activate the existing window (single-instance), not spawn a duplicate.
/// </summary>
public sealed class ShellPageToolWindowReopenUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenPreferencesTwice_FromFileMenu_StillExposesSinglePreferencesWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var first = shell.OpenToolWindow("File", "Preferences...", "Preferences");
        try
        {
            await Assert.That(first.GetTitle()).IsEqualTo("Preferences");

            // Opening the same tool window a second time activates the existing
            // instance (per AvaloniaToolWindowOpener). The shell should still
            // be responsive, and a single Preferences window remains.
            using var second = shell.OpenToolWindow("File", "Preferences...", "Preferences");
            try
            {
                await Assert.That(second.GetTitle()).IsEqualTo("Preferences");
            }
            finally
            {
                second.Close();
            }
        }
        finally
        {
            first.Close();
        }
    }

    [Test]
    public async Task OpenBlockListTwice_FromToolsMenu_StillExposesSingleBlockListWindow()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var first = shell.OpenToolWindow("Tools", "Block List...", "Block List");
        try
        {
            await Assert.That(first.GetTitle()).IsEqualTo("Block List");

            using var second = shell.OpenToolWindow("Tools", "Block List...", "Block List");
            try
            {
                await Assert.That(second.GetTitle()).IsEqualTo("Block List");
            }
            finally
            {
                second.Close();
            }
        }
        finally
        {
            first.Close();
        }
    }
}
