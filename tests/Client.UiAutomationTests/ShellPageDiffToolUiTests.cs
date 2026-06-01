using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end FlaUI tests for the Diff Tool window opened from
///     <c>Tools → Diff Tool...</c> (<c>docs/DESIGN.md § 6.14 Diff Tool</c>).
/// </summary>
public sealed class ShellPageDiffToolUiTests : UiAutomationTestBase
{
    [Test]
    public async Task OpenDiffTool_FromToolsMenu_ShowsDiffToolWindowWithCoreControls()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var diff = shell.OpenToolWindow("Tools", "Diff Tool...", "Diff Tool");
        try
        {
            await Assert.That(diff.GetTitle()).IsEqualTo("Diff Tool");
            await Assert.That(diff.ListBoxByName("Left diff selection")).IsNotNull();
            await Assert.That(diff.ListBoxByName("Right diff selection")).IsNotNull();
            await Assert.That(diff.HasButton("Clear Pool")).IsTrue();
        }
        finally
        {
            diff.Close();
        }
    }

    [Test]
    public async Task ClearPoolButton_EmptyDiffTool_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var diff = shell.OpenToolWindow("Tools", "Diff Tool...", "Diff Tool");
        try
        {
            diff.Button("Clear Pool").Click();

            // The window must still be responsive after the (no-op) clear.
            await Assert.That(diff.GetTitle()).IsEqualTo("Diff Tool");
            await Assert.That(diff.HasButton("Clear Pool")).IsTrue();
        }
        finally
        {
            diff.Close();
        }
    }

    [Test]
    public async Task RemoveSelectedButton_EmptyDiffTool_LeavesWindowResponsive()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var diff = shell.OpenToolWindow("Tools", "Diff Tool...", "Diff Tool");
        try
        {
            // With no selection the Remove Selected button is a safe no-op.
            diff.Button("Remove Selected").Click();

            await Assert.That(diff.GetTitle()).IsEqualTo("Diff Tool");
            await Assert.That(diff.HasButton("Clear Pool")).IsTrue();
        }
        finally
        {
            diff.Close();
        }
    }

    [Test]
    public async Task DiffBox_FreshDiffTool_IsDiscoverable()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        using var diff = shell.OpenToolWindow("Tools", "Diff Tool...", "Diff Tool");
        try
        {
            // The computed-diff textbox is always present, even when empty.
            await Assert.That(diff.TextBoxByName("Computed diff")).IsNotNull();
        }
        finally
        {
            diff.Close();
        }
    }
}
