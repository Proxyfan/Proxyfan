using System.Globalization;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Formats raw byte counts as compact human-readable strings (B / KB / MB) using
///     binary (1024) units. Used by the WebSocket message list to surface payload size.
/// </summary>
public static class WebSocketByteSizeFormatter
{
    /// <summary>
    ///     Returns a short, culture-invariant representation of the supplied byte count.
    /// </summary>
    /// <param name="byteCount">The byte count to format. Negative inputs are formatted as-is.</param>
    /// <returns>A formatted string such as <c>"12 B"</c>, <c>"3.4 KB"</c>, or <c>"1.2 MB"</c>.</returns>
    public static string Format(int byteCount)
    {
        if (byteCount < 1024)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} B", byteCount);
        }

        if (byteCount < 1024 * 1024)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1} KB",
                byteCount / 1024.0);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:F1} MB",
            byteCount / (1024.0 * 1024.0));
    }
}
