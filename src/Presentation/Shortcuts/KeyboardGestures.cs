namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Helpers for <see cref="KeyboardGesture" /> normalization.
/// </summary>
public static class KeyboardGestures
{
    /// <summary>
    ///     Normalizes a key string by upper-casing single ASCII-letter keys so that
    ///     gesture lookups remain case-insensitive for letters. Multi-character key names
    ///     such as <c>"Delete"</c> or <c>"F1"</c> are preserved verbatim.
    /// </summary>
    /// <param name="value">The key text to normalize.</param>
    /// <returns>The normalized key text.</returns>
    public static string NormalizeKey(string value)
    {
        if (value.Length == 1 && value[0] >= 'a' && value[0] <= 'z')
        {
            return char.ToUpperInvariant(value[0]).ToString();
        }

        return value;
    }
}
