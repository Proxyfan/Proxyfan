namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     A keyboard gesture (modifier + key) represented in a platform-neutral way so that
///     <see cref="ShortcutRegistry" /> can store and validate user bindings without depending
///     on UI framework types.
/// </summary>
public sealed class KeyboardGesture
{
    /// <summary>
    ///     Gets the primary key (without modifiers), normalized to upper-case for letters.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    ///     Gets the modifier mask combining one or more <see cref="KeyboardModifier" /> flags.
    /// </summary>
    public required KeyboardModifier Modifiers { get; init; }

    /// <summary>
    ///     Renders the gesture as a human-readable string like <c>Ctrl+Shift+B</c>.
    /// </summary>
    /// <returns>The formatted gesture text.</returns>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();

        if (Modifiers.HasFlag(KeyboardModifier.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(KeyboardModifier.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(KeyboardModifier.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(KeyboardModifier.Meta))
        {
            parts.Add("Meta");
        }

        parts.Add(Key);
        return string.Join("+", parts);
    }
}
