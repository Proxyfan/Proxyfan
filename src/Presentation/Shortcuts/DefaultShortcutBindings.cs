using System.Collections.Generic;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Static helper that constructs the default shortcut bindings used by
///     <see cref="ShortcutRegistry" />. Mirrors the gestures from the design spec.
/// </summary>
public static class DefaultShortcutBindings
{
    /// <summary>
    ///     Builds the default action-to-gesture mapping.
    /// </summary>
    /// <returns>A mutable dictionary that callers may further modify.</returns>
    public static Dictionary<ShortcutAction, KeyboardGesture> Build()
    {
        var bindings = new Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.ToggleCapture] = BuildGesture("R", KeyboardModifier.Control),
            [ShortcutAction.ClearTraffic] = BuildGesture("K", KeyboardModifier.Control),
            [ShortcutAction.Find] = BuildGesture("F", KeyboardModifier.Control),
            [ShortcutAction.ToggleNoCaching] = BuildGesture("N", KeyboardModifier.Control | KeyboardModifier.Shift),
            [ShortcutAction.ToggleBreakpoint] = BuildGesture("B", KeyboardModifier.Control | KeyboardModifier.Shift),
            [ShortcutAction.ExportSession] = BuildGesture("E", KeyboardModifier.Control),
            [ShortcutAction.RemoveSelected] = BuildGesture("Delete", KeyboardModifier.None),
        };
        return bindings;
    }

    private static KeyboardGesture BuildGesture(string key, KeyboardModifier modifiers)
    {
        var gesture = new KeyboardGesture
        {
            Key = key,
            Modifiers = modifiers,
        };
        return gesture;
    }
}
