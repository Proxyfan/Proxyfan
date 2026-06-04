using System.Collections.Generic;

namespace Proxyfan.Cli;

/// <summary>
///     Machine-readable aggregated statistics for a HAR capture.
/// </summary>
public sealed class HarStatsReport
{
    /// <summary>
    ///     Gets the request duration summary in milliseconds, or <see langword="null" /> when no
    ///     timing samples were available.
    /// </summary>
    public HarStatsDurationSummary? DurationMilliseconds { get; init; }

    /// <summary>
    ///     Gets the request-method distribution.
    /// </summary>
    public required IReadOnlyDictionary<string, int> Methods { get; init; }

    /// <summary>
    ///     Gets the total bytes across all request bodies.
    /// </summary>
    public long RequestBodyBytes { get; init; }

    /// <summary>
    ///     Gets the total bytes across all response bodies.
    /// </summary>
    public long ResponseBodyBytes { get; init; }

    /// <summary>
    ///     Gets the response-status-class distribution.
    /// </summary>
    public required IReadOnlyDictionary<string, int> StatusClasses { get; init; }

    /// <summary>
    ///     Gets the total number of flows in the HAR.
    /// </summary>
    public int TotalFlows { get; init; }
}
