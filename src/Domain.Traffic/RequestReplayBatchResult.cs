namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Summary for a multi-request replay operation.
/// </summary>
public sealed record RequestReplayBatchResult
{
    /// <summary>
    ///     Gets how many replay attempts completed successfully.
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    ///     Gets how many replay attempts were requested.
    /// </summary>
    public int RequestedCount { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequestReplayBatchResult" /> record.
    /// </summary>
    /// <param name="completedCount">How many replay attempts completed successfully.</param>
    /// <param name="requestedCount">How many replay attempts were requested.</param>
    public RequestReplayBatchResult(int completedCount, int requestedCount)
    {
        CompletedCount = completedCount;
        RequestedCount = requestedCount;
    }
}
