using System.Globalization;
using System.Resources;
using System.Threading.Tasks;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ShortcutActionLabels" />.
/// </summary>
public sealed class ShortcutActionLabelsTests
{
    /// <summary>
    ///     GetResourceKey returns the expected resource key for each known action.
    /// </summary>
    [Test]
    [Arguments(ShortcutAction.ToggleCapture, "Tools_KeyboardShortcuts_Action_ToggleCapture")]
    [Arguments(ShortcutAction.ClearTraffic, "Tools_KeyboardShortcuts_Action_ClearTraffic")]
    [Arguments(ShortcutAction.Find, "Tools_KeyboardShortcuts_Action_Find")]
    [Arguments(ShortcutAction.ToggleNoCaching, "Tools_KeyboardShortcuts_Action_ToggleNoCaching")]
    [Arguments(ShortcutAction.ToggleBreakpoint, "Tools_KeyboardShortcuts_Action_ToggleBreakpoint")]
    [Arguments(ShortcutAction.ExportSession, "Tools_KeyboardShortcuts_Action_ExportSession")]
    [Arguments(ShortcutAction.RemoveSelected, "Tools_KeyboardShortcuts_Action_RemoveSelected")]
    public async Task GetResourceKey_KnownAction_ReturnsExpectedKey(ShortcutAction action, string expected)
    {
        var key = ShortcutActionLabels.GetResourceKey(action);

        await Assert.That(key).IsEqualTo(expected);
    }

    /// <summary>
    ///     GetResourceKey returns the unknown-action key for action values that have no mapping.
    /// </summary>
    [Test]
    public async Task GetResourceKey_UnknownAction_ReturnsUnknownKey()
    {
        var key = ShortcutActionLabels.GetResourceKey((ShortcutAction)999);

        await Assert.That(key).IsEqualTo(ShortcutActionLabels.UnknownResourceKey);
    }

    /// <summary>
    ///     GetLabel returns the localized text from the Client Strings resource table
    ///     for every defined <see cref="ShortcutAction" />.
    /// </summary>
    [Test]
    [Arguments(ShortcutAction.ToggleCapture, "Toggle capture")]
    [Arguments(ShortcutAction.ClearTraffic, "Clear traffic")]
    [Arguments(ShortcutAction.Find, "Find")]
    [Arguments(ShortcutAction.ToggleNoCaching, "Toggle No-Caching")]
    [Arguments(ShortcutAction.ToggleBreakpoint, "Toggle breakpoint")]
    [Arguments(ShortcutAction.ExportSession, "Export session")]
    [Arguments(ShortcutAction.RemoveSelected, "Remove selected")]
    public async Task GetLabel_KnownAction_ReturnsLocalizedText(ShortcutAction action, string expected)
    {
        var localization = CreateLocalizationServiceWithClientResources();

        var label = ShortcutActionLabels.GetLabel(action, localization);

        await Assert.That(label).IsEqualTo(expected);
    }

    /// <summary>
    ///     GetLabel resolves the unknown-action resource key for action values that have no mapping.
    /// </summary>
    [Test]
    public async Task GetLabel_UnknownAction_ReturnsLocalizedUnknown()
    {
        var localization = CreateLocalizationServiceWithClientResources();

        var label = ShortcutActionLabels.GetLabel((ShortcutAction)999, localization);

        await Assert.That(label).IsEqualTo("Unknown action");
    }

    private static LocalizationService CreateLocalizationServiceWithClientResources()
    {
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        var clientAssembly = typeof(Proxyfan.Client.App).Assembly;
        var manager = new ResourceManager("Proxyfan.Client.Resources.Strings", clientAssembly);
        localization.RegisterManager(manager);
        return localization;
    }
}
