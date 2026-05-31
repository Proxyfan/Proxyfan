using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     End-to-end UI automation tests that drive the real Proxyfan shell window
///     using FlaUI's mouse + keyboard simulation. Each test launches a fresh
///     <c>Client.Desktop.exe</c> process with isolated user data and runs
///     sequentially (enforced by <see cref="UiAutomationTestBase" />).
///     <para>
///         Covers <c>docs/DESIGN.md § 4 Application Layout</c>, § 6.1 Traffic Capture,
///         § 6.4 Traffic Filtering, and § 6.25 Multiple Tabs.
///     </para>
/// </summary>
public sealed class ShellPageUiTests : UiAutomationTestBase
{
    [Test]
    public async Task Launch_FreshProcess_ExposesAllPrimaryShellElements()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        await Assert.That(shell.GetTitle()).IsEqualTo("Proxyfan");
        await Assert.That(shell.FilterTextBox()).IsNotNull();
        await Assert.That(shell.SourceList()).IsNotNull();
        await Assert.That(shell.TabList()).IsNotNull();
        await Assert.That(shell.NewTabButton()).IsNotNull();
    }

    [Test]
    public async Task ClickPauseCapture_WhileCapturing_SwapsButtonToResumeCapture()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var pauseButton = shell.ToolbarButton("Pause Capture");
        pauseButton.Click();

        shell.WaitUntil(
            () => TryFind(shell, "Resume Capture"),
            description: "the toolbar to swap to 'Resume Capture'");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickResumeCapture_AfterPausing_SwapsButtonBackToPauseCapture()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Pause Capture").Click();
        shell.WaitUntil(() => TryFind(shell, "Resume Capture"), "Resume button visible");

        shell.ToolbarButton("Resume Capture").Click();
        shell.WaitUntil(() => TryFind(shell, "Pause Capture"), "Pause button visible after resume");

        await Task.CompletedTask;
    }

    [Test]
    public async Task TypeIntoFilterTextBox_TypedSubstring_PropagatesIntoControlText()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filterTextBox = shell.FilterTextBox();
        filterTextBox.Focus();
        Keyboard.Type("example.com");

        shell.WaitUntil(
            () => string.Equals(filterTextBox.Text, "example.com", StringComparison.Ordinal),
            description: "filter textbox to contain 'example.com'");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickNewTabButton_FreshShell_AppendsAdditionalTab()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var tabsBefore = shell.TabList().Items.Length;
        shell.NewTabButton().Click();

        shell.WaitUntil(
            () => shell.TabList().Items.Length == tabsBefore + 1,
            description: $"tab strip to grow from {tabsBefore} to {tabsBefore + 1}");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ClickClearButton_EmptyList_LeavesToolbarFunctional()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Clear").Click();

        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
    }

    [Test]
    public async Task PressCtrlR_FreshShell_TogglesCaptureToPaused()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);
        shell.Window.Focus();

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_R);

        shell.WaitUntil(() => TryFind(shell, "Resume Capture"), "Resume button visible after Ctrl+R");

        await Task.CompletedTask;
    }

    private static bool TryFind(ShellPage shell, string label)
    {
        try
        {
            return shell.ToolbarButton(label) is { IsEnabled: true };
        }
        catch
        {
            return false;
        }
    }
}
