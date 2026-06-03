namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Strongly-typed view of the user-editable preferences that are surfaced through the
///     Preferences UI. Captures the subset of configuration that an end-user can change at
///     runtime: proxy port, upstream proxy settings, theme, locale, capture cap.
/// </summary>
public sealed class UserPreferences
{
    /// <summary>
    ///     Gets the maximum number of captured traffic flows the in-memory store retains before
    ///     LRU eviction. Valid range: 100-1,000,000. Default: 10,000.
    /// </summary>
    public required int CaptureMaximumFlows { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the proxy should register itself as the system
    ///     proxy on Windows on startup. Default: true.
    /// </summary>
    public required bool IsRegisterSystemProxyOnStartup { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the proxy should start automatically when the
    ///     application launches. Default: true.
    /// </summary>
    public required bool IsStartProxyOnLaunch { get; init; }

    /// <summary>
    ///     Gets a value indicating whether upstream proxy forwarding is enabled.
    /// </summary>
    public required bool IsUpstreamProxyEnabled { get; init; }

    /// <summary>
    ///     Gets the locale (e.g. "en-US", "fr-FR"). Null or empty falls back to the Windows
    ///     system locale.
    /// </summary>
    public required string? Locale { get; init; }

    /// <summary>
    ///     Gets the log level: Trace, Debug, Information, Warning, Error.
    ///     Default: Information.
    /// </summary>
    public required string LogLevel { get; init; }

    /// <summary>
    ///     Gets the TCP port the proxy listener binds to. Valid range: 1024-65535.
    ///     Default: 8080. Requires a restart to apply.
    /// </summary>
    public required int ProxyPort { get; init; }

    /// <summary>
    ///     Gets the application theme: System, Light, Dark.
    /// </summary>
    public required string Theme { get; init; }

    /// <summary>
    ///     Gets the upstream proxy host. Null when upstream proxy is disabled.
    /// </summary>
    public required string? UpstreamProxyHost { get; init; }

    /// <summary>
    ///     Gets the upstream proxy port. Valid range: 1-65535 when upstream proxy is enabled.
    /// </summary>
    public required int UpstreamProxyPort { get; init; }
}
