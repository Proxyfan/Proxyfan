namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Validation helpers for <see cref="UserPreferences" /> ranges.
/// </summary>
public static class UserPreferencesValidation
{
    /// <summary>
    ///     Maximum allowed capture-flow retention count.
    /// </summary>
    public const int CaptureMaximumFlowsMaximum = 1_000_000;

    /// <summary>
    ///     Minimum allowed capture-flow retention count.
    /// </summary>
    public const int CaptureMaximumFlowsMinimum = 100;

    /// <summary>
    ///     Returns whether the capture-flow retention count is within supported bounds.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true" /> when the value is valid.</returns>
    public static bool HasValidCaptureMaximumFlows(int value)
    {
        return value is >= CaptureMaximumFlowsMinimum and <= CaptureMaximumFlowsMaximum;
    }
}
