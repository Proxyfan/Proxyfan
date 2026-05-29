using System.Threading.Tasks;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ShortcutActionLabels" />.
/// </summary>
public sealed class ShortcutActionLabelsTests
{
    /// <summary>
    ///     GetLabel returns a non-empty label for every defined <see cref="ShortcutAction" />.
    /// </summary>
    [Test]
    public async Task GetLabel_AllActions_ReturnsNonEmptyLabel()
    {
        foreach (var action in System.Enum.GetValues<ShortcutAction>())
        {
            var label = ShortcutActionLabels.GetLabel(action);

            await Assert.That(label).IsNotEmpty();
        }
    }

    /// <summary>
    ///     GetLabel returns the expected text for each known action.
    /// </summary>
    [Test]
    [Arguments(ShortcutAction.ToggleCapture, "Toggle capture")]
    [Arguments(ShortcutAction.ClearTraffic, "Clear traffic")]
    [Arguments(ShortcutAction.Find, "Find")]
    [Arguments(ShortcutAction.ToggleNoCaching, "Toggle No-Caching")]
    [Arguments(ShortcutAction.ToggleBreakpoint, "Toggle breakpoint")]
    [Arguments(ShortcutAction.ExportSession, "Export session")]
    [Arguments(ShortcutAction.RemoveSelected, "Remove selected")]
    public async Task GetLabel_KnownAction_ReturnsExpectedText(ShortcutAction action, string expected)
    {
        var label = ShortcutActionLabels.GetLabel(action);

        await Assert.That(label).IsEqualTo(expected);
    }

    /// <summary>
    ///     GetLabel falls back to <c>ToString()</c> for unknown action values.
    /// </summary>
    [Test]
    public async Task GetLabel_UnknownAction_FallsBackToEnumName()
    {
        var label = ShortcutActionLabels.GetLabel((ShortcutAction)999);

        await Assert.That(label).IsEqualTo("999");
    }
}
