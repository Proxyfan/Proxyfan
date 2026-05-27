namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Action identifiers used by the shortcut registry. These are language-neutral keys
///     that can be bound to specific keyboard gestures via <see cref="ShortcutRegistry" />.
/// </summary>
public enum ShortcutAction
{
    /// <summary>
    ///     Toggle capture on/off.
    /// </summary>
    ToggleCapture,

    /// <summary>
    ///     Clear all captured traffic flows.
    /// </summary>
    ClearTraffic,

    /// <summary>
    ///     Open the find/filter input.
    /// </summary>
    Find,

    /// <summary>
    ///     Toggle the No-Caching tool.
    /// </summary>
    ToggleNoCaching,

    /// <summary>
    ///     Toggle the breakpoint tool.
    /// </summary>
    ToggleBreakpoint,

    /// <summary>
    ///     Export the current session.
    /// </summary>
    ExportSession,

    /// <summary>
    ///     Remove the selected traffic flow.
    /// </summary>
    RemoveSelected,
}
