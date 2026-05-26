using System;

namespace Proxyfan.Domain.Traffic.Events;

/// <summary>
///     Published when a traffic flow reaches a terminal status.
/// </summary>
public sealed class TrafficFlowCompleted : IDomainEvent
{
    /// <summary>
    ///     Gets the terminal flow status.
    /// </summary>
    public TrafficFlowStatus Status { get; }

    /// <summary>
    ///     Gets the UTC instant at which the flow completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Gets the traffic flow identifier.
    /// </summary>
    public Guid TrafficFlowId { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficFlowCompleted" /> instance.
    /// </summary>
    /// <param name="trafficFlowId">
    ///     The traffic flow identifier.
    /// </param>
    /// <param name="status">
    ///     The terminal flow status.
    /// </param>
    /// <param name="timestamp">
    ///     The UTC instant at which the flow completed.
    /// </param>
    public TrafficFlowCompleted(Guid trafficFlowId, TrafficFlowStatus status, DateTimeOffset timestamp)
    {
        Status = status;
        Timestamp = timestamp;
        TrafficFlowId = trafficFlowId;
    }
}