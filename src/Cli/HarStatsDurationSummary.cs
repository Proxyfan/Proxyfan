namespace Proxyfan.Cli;

/// <summary>
///     Machine-readable duration summary for a HAR capture.
/// </summary>
public sealed class HarStatsDurationSummary
{
    /// <summary>
    ///     Gets the maximum request duration in milliseconds.
    /// </summary>
    public double Max { get; init; }

    /// <summary>
    ///     Gets the median request duration in milliseconds.
    /// </summary>
    public double Median { get; init; }

    /// <summary>
    ///     Gets the minimum request duration in milliseconds.
    /// </summary>
    public double Min { get; init; }

    /// <summary>
    ///     Gets the number of timing samples represented by the summary.
    /// </summary>
    public int Samples { get; init; }
}
