namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Validation helpers for user-editable preference ranges.
/// </summary>
public static class UserPreferencesValidation
{
    /// <summary>
    ///     Maximum allowed retained traffic flow count.
    /// </summary>
    public const int MaximumCaptureMaximumFlows = 1_000_000;

    /// <summary>
    ///     Minimum allowed retained traffic flow count.
    /// </summary>
    public const int MinimumCaptureMaximumFlows = 100;

    /// <summary>
    ///     Returns whether the supplied capture-flow cap is in the supported range.
    /// </summary>
    /// <param name="captureMaximumFlows">The retained-flow cap to validate.</param>
    /// <returns><see langword="true" /> when valid; otherwise <see langword="false" />.</returns>
    public static bool HasValidCaptureMaximumFlows(int captureMaximumFlows)
    {
        return captureMaximumFlows is >= MinimumCaptureMaximumFlows and <= MaximumCaptureMaximumFlows;
    }
}
