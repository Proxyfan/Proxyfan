using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Immutable point-in-time snapshot of a <see cref="ServerSentEventsFlow" />, produced
///     atomically under the producer lock by
///     <see cref="ServerSentEventsFlow.GetEventsSnapshot" />. Allows observers to seed their
///     initial state from a stable view without racing against concurrent
///     <see cref="ServerSentEventsFlow.RecordEvent" /> calls.
/// </summary>
public sealed record ServerSentEventsFlowSnapshot
{
    /// <summary>
    ///     Gets the chronological list of events captured at snapshot time.
    /// </summary>
    public required IReadOnlyList<ServerSentEvent> Events { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the stream had already been observed to close at
    ///     the time the snapshot was taken.
    /// </summary>
    public required bool IsClosed { get; init; }
}
