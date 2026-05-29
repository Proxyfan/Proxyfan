using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Static helper that maps <see cref="ShortcutAction" /> values to human-readable
///     labels used in the shortcut customization grid. Kept separate from the view model
///     so that the mapping can be unit-tested without instantiating UI types.
/// </summary>
public static class ShortcutActionLabels
{
    /// <summary>
    ///     Returns the human-readable label for the supplied action.
    /// </summary>
    /// <param name="action">The action to label.</param>
    /// <returns>The label text.</returns>
    public static string GetLabel(ShortcutAction action)
    {
        if (action == ShortcutAction.ToggleCapture)
        {
            return "Toggle capture";
        }

        if (action == ShortcutAction.ClearTraffic)
        {
            return "Clear traffic";
        }

        if (action == ShortcutAction.Find)
        {
            return "Find";
        }

        if (action == ShortcutAction.ToggleNoCaching)
        {
            return "Toggle No-Caching";
        }

        if (action == ShortcutAction.ToggleBreakpoint)
        {
            return "Toggle breakpoint";
        }

        if (action == ShortcutAction.ExportSession)
        {
            return "Export session";
        }

        if (action == ShortcutAction.RemoveSelected)
        {
            return "Remove selected";
        }

        return action.ToString();
    }
}
