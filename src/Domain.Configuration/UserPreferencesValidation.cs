namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Validation helpers for user preference value ranges shared by UI and persistence.
/// </summary>
public static class UserPreferencesValidation
{
    /// <summary>
    ///     The maximum capture-flow retention cap accepted by preferences.
    /// </summary>
    public const int MaximumCaptureMaximumFlows = 1_000_000;

    /// <summary>
    ///     The minimum capture-flow retention cap accepted by preferences.
    /// </summary>
    public const int MinimumCaptureMaximumFlows = 100;

    /// <summary>
    ///     Returns <see langword="true" /> when the value is within the supported capture-flow range.
    /// </summary>
    /// <param name="value">The capture-flow cap to validate.</param>
    /// <returns><see langword="true" /> when the value is valid.</returns>
    public static bool HasValidCaptureMaximumFlows(int value)
    {
        if (value is < MinimumCaptureMaximumFlows or > MaximumCaptureMaximumFlows)
        {
            return false;
        }

        return true;
    }
}
