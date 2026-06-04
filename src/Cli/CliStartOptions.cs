namespace Proxyfan.Cli;

/// <summary>
///     Parsed options for the <see cref="CliCommandKind.Start" /> command. Bundles the
///     optional <c>--output</c> path (HAR export on shutdown) and the optional
///     <c>--duration</c> seconds (auto-stop after the elapsed time).
/// </summary>
public sealed class CliStartOptions
{
    /// <summary>
    ///     Gets the optional bind address for the listener (<c>proxy.bindAddress</c> override).
    ///     When null, the configured/default bind address is used.
    /// </summary>
    public string? BindAddress { get; init; }

    /// <summary>
    ///     Gets the optional auto-stop duration in seconds. When set, the proxy stops
    ///     automatically after this many seconds even if no Ctrl+C is observed.
    /// </summary>
    public int? DurationSeconds { get; init; }

    /// <summary>
    ///     Gets the optional HAR output path. When set, the captured flows are exported to
    ///     this file in HAR 1.2 format when the proxy stops.
    /// </summary>
    public string? OutputPath { get; init; }
}
