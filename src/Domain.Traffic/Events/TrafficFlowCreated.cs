using System;

namespace Proxyfan.Domain.Traffic.Events;

/// <summary>
///     Published when a new traffic flow has been created.
/// </summary>
public sealed class TrafficFlowCreated : IDomainEvent
{
    /// <summary>
    ///     Gets the UTC instant at which the flow was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Gets the traffic flow identifier.
    /// </summary>
    public Guid TrafficFlowId { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowCreated" /> instance.
    /// </summary>
    /// <param name="trafficFlowId">
    ///     The traffic flow identifier.
    /// </param>
    /// <param name="timestamp">
    ///     The UTC instant at which the flow was created.
    /// </param>
    public TrafficFlowCreated(Guid trafficFlowId, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        TrafficFlowId = trafficFlowId;
    }
}