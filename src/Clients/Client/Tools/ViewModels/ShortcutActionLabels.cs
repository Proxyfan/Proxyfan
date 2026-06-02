using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Static helper that maps <see cref="ShortcutAction" /> values to localized
///     labels used in the shortcut customization grid. Labels are sourced from the
///     client Resources table so they follow the configured locale and runtime
///     language changes; unknown action values fall back to a localized
///     "Unknown action" string.
/// </summary>
public static class ShortcutActionLabels
{
    /// <summary>
    ///     Resource key returned for <see cref="ShortcutAction" /> values that do not
    ///     have a dedicated mapping.
    /// </summary>
    public const string UnknownResourceKey = "Tools_KeyboardShortcuts_Action_Unknown";

    /// <summary>
    ///     Returns the localized label for the supplied action by resolving its
    ///     resource key through the supplied <see cref="LocalizationService" />.
    /// </summary>
    /// <param name="action">The action to label.</param>
    /// <param name="localization">The localization service used to resolve the label.</param>
    /// <returns>The localized label text.</returns>
    public static string GetLabel(ShortcutAction action, LocalizationService localization)
    {
        var key = GetResourceKey(action);
        return localization[key];
    }

    /// <summary>
    ///     Returns the resource key used to look up the localized label for the
    ///     supplied action. Unknown action values resolve to
    ///     <see cref="UnknownResourceKey" />.
    /// </summary>
    /// <param name="action">The action to map.</param>
    /// <returns>The resource key that identifies the localized label.</returns>
    public static string GetResourceKey(ShortcutAction action)
    {
        if (action == ShortcutAction.ToggleCapture)
        {
            return "Tools_KeyboardShortcuts_Action_ToggleCapture";
        }

        if (action == ShortcutAction.ClearTraffic)
        {
            return "Tools_KeyboardShortcuts_Action_ClearTraffic";
        }

        if (action == ShortcutAction.Find)
        {
            return "Tools_KeyboardShortcuts_Action_Find";
        }

        if (action == ShortcutAction.ToggleNoCaching)
        {
            return "Tools_KeyboardShortcuts_Action_ToggleNoCaching";
        }

        if (action == ShortcutAction.ToggleBreakpoint)
        {
            return "Tools_KeyboardShortcuts_Action_ToggleBreakpoint";
        }

        if (action == ShortcutAction.ExportSession)
        {
            return "Tools_KeyboardShortcuts_Action_ExportSession";
        }

        if (action == ShortcutAction.RemoveSelected)
        {
            return "Tools_KeyboardShortcuts_Action_RemoveSelected";
        }

        return UnknownResourceKey;
    }
}
