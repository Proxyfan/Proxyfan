using System;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Modifier-key flags for <see cref="KeyboardGesture" />.
/// </summary>
[Flags]
public enum KeyboardModifier
{
    /// <summary>
    ///     No modifier keys are held.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Control key is held.
    /// </summary>
    Control = 1,

    /// <summary>
    ///     Shift key is held.
    /// </summary>
    Shift = 2,

    /// <summary>
    ///     Alt key is held.
    /// </summary>
    Alt = 4,

    /// <summary>
    ///     Meta (Win/Cmd) key is held.
    /// </summary>
    Meta = 8,
}
