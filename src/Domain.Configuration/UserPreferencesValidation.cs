namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Validation helpers for <see cref="UserPreferences" /> values shared between UI and
///     persistence layers.
/// </summary>
public static class UserPreferencesValidation
{
    /// <summary>
    ///     Maximum supported capture-flow capacity.
    /// </summary>
    public const int CaptureMaximumFlowsMaximum = 1_000_000;

    /// <summary>
    ///     Minimum supported capture-flow capacity.
    /// </summary>
    public const int CaptureMaximumFlowsMinimum = 100;

    /// <summary>
    ///     Returns whether the capture-flow capacity is inside the supported range.
    /// </summary>
    /// <param name="value">Requested capture-flow capacity.</param>
    /// <returns><see langword="true" /> when valid.</returns>
    public static bool HasValidCaptureMaximumFlows(int value)
    {
        return value is >= CaptureMaximumFlowsMinimum and <= CaptureMaximumFlowsMaximum;
    }
}
