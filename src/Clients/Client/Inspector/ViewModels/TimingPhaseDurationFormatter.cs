namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     Formats a phase duration in milliseconds as a human-readable label.
/// </summary>
public static class TimingPhaseDurationFormatter
{
    /// <summary>
    ///     Formats <paramref name="milliseconds" /> as <c>"F2"</c> with a trailing
    ///     <c>" ms"</c> suffix using the invariant culture.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <returns>The formatted duration label.</returns>
    public static string Format(double milliseconds)
    {
        return milliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " ms";
    }
}
