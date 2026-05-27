namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Factory for the documented default <see cref="UserPreferences" /> values shipped with
///     Proxyfan. Kept separate from the entity itself to satisfy the analyzer rule that
///     forbids static helpers on non-static classes.
/// </summary>
public static class UserPreferencesDefaults
{
    /// <summary>
    ///     Returns a <see cref="UserPreferences" /> populated with the documented default
    ///     values (port 8080, capture cap 10,000, theme System, log level Information,
    ///     upstream disabled).
    /// </summary>
    /// <returns>The default preferences.</returns>
    public static UserPreferences Create()
    {
        var defaults = new UserPreferences
        {
            CaptureMaximumFlows = 10_000,
            IsRegisterSystemProxyOnStartup = true,
            IsStartProxyOnLaunch = true,
            IsUpstreamProxyEnabled = false,
            Locale = null,
            LogLevel = "Information",
            ProxyPort = 8080,
            Theme = "System",
            UpstreamProxyHost = null,
            UpstreamProxyPort = 8080,
        };
        return defaults;
    }
}
