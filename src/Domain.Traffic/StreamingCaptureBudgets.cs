namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Default buffer and budget values for streaming protocol captures.
/// </summary>
public static class StreamingCaptureBudgets
{
    /// <summary>
    ///     Default shared global streaming capture budget (200 MB).
    /// </summary>
    public const long GlobalBudgetBytes = 200L * 1024L * 1024L;

    /// <summary>
    ///     Default retained event capacity for Server-Sent Events flows.
    /// </summary>
    public const int ServerSentEventsEventCapacity = 5000;

    /// <summary>
    ///     Default retained message capacity for WebSocket and gRPC flows.
    /// </summary>
    public const int WebSocketAndRemoteProcedureCallMessageCapacity = 1000;

    /// <summary>
    ///     Gets the shared global streaming capture budget instance.
    /// </summary>
    public static StreamingCaptureBudget Shared { get; }

    static StreamingCaptureBudgets()
    {
        var shared = new StreamingCaptureBudget(GlobalBudgetBytes);
        Shared = shared;
    }
}
