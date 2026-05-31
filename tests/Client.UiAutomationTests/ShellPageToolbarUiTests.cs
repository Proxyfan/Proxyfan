using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Proxyfan.Client.UiAutomationTests.Infrastructure;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.UiAutomationTests;

/// <summary>
///     Additional end-to-end FlaUI automation tests covering the toolbar +
///     filter behaviour from <c>docs/DESIGN.md § 4.6 Toolbar</c> and § 6.4
///     Traffic Filtering. Every test launches a fresh isolated Proxyfan
///     process and drives the live UI via mouse / keyboard.
/// </summary>
public sealed class ShellPageToolbarUiTests : UiAutomationTestBase
{
    [Test]
    public async Task PauseCaptureThenResume_SecondToggle_RestoresPauseLabel()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        shell.ToolbarButton("Pause Capture").Click();
        shell.WaitUntil(() => Exists(shell, "Resume Capture"), "Resume Capture visible");
        shell.ToolbarButton("Resume Capture").Click();
        shell.WaitUntil(() => Exists(shell, "Pause Capture"), "Pause Capture visible");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PauseCaptureFourTimes_FourthPress_LeavesPauseLabelVisible()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        // Pause - Resume - Pause - Resume → start at Pause, end at Pause.
        for (var i = 0; i < 2; i++)
        {
            shell.ToolbarButton("Pause Capture").Click();
            shell.WaitUntil(() => Exists(shell, "Resume Capture"), "Resume visible");
            shell.ToolbarButton("Resume Capture").Click();
            shell.WaitUntil(() => Exists(shell, "Pause Capture"), "Pause visible");
        }

        await Assert.That(Exists(shell, "Pause Capture")).IsTrue();
    }

    [Test]
    public async Task TypeIntoFilter_ThenBackspaceAllCharacters_EmptiesTheTextBox()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        var filter = shell.FilterTextBox();
        filter.Focus();
        Keyboard.Type("foo.example.com");
        shell.WaitUntil(() => string.Equals(filter.Text, "foo.example.com", System.StringComparison.Ordinal), "text populated");

        // Real user clears the field by Backspace-ing through every character.
        // This is the most reliable cross-IME gesture supported by Avalonia
        // TextBox and works without depending on Ctrl+A accelerators.
        for (var i = 0; i < "foo.example.com".Length; i++)
        {
            Keyboard.Type(VirtualKeyShort.BACK);
        }

        shell.WaitUntil(() => string.IsNullOrEmpty(filter.Text), "filter text cleared");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ToolbarButtons_FreshShell_AllStandardButtonsVisibleAndEnabled()
    {
        await using var app = ProxyfanApp.Launch();
        var shell = new ShellPage(app);

        await Assert.That(shell.ToolbarButton("Pause Capture").IsEnabled).IsTrue();
        await Assert.That(shell.ToolbarButton("Clear").IsEnabled).IsTrue();
        await Assert.That(shell.ToolbarButton("Open Session...").IsEnabled).IsTrue();
        await Assert.That(shell.ToolbarButton("Save Session...").IsEnabled).IsTrue();
        await Assert.That(shell.ToolbarButton("Enable Proxy").IsEnabled).IsTrue();
    }

    private static bool Exists(ShellPage shell, string label)
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
