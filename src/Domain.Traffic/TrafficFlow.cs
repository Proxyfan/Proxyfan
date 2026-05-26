using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a single proxy traffic flow — one client connection from acceptance
///     to completion or failure.
/// </summary>
/// <param name="id">A unique identifier for this flow.</param>
/// <param name="clientEndPoint">The string representation of the client's remote endpoint.</param>
/// <param name="startedAt">The UTC instant at which the connection was accepted.</param>
public sealed class TrafficFlow(Guid id, string clientEndPoint, DateTimeOffset startedAt)
{
    /// <summary>Gets the unique identifier of this flow.</summary>
    public Guid Id { get; } = id;

    /// <summary>Gets the string representation of the client's remote endpoint (e.g., <c>"127.0.0.1:54321"</c>).</summary>
    public string ClientEndPoint { get; } = clientEndPoint;

    /// <summary>Gets the UTC instant at which the connection was accepted.</summary>
    public DateTimeOffset StartedAt { get; } = startedAt;

    /// <summary>Gets the current lifecycle status of this flow.</summary>
    public TrafficFlowStatus Status { get; private set; } = TrafficFlowStatus.Pending;

    /// <summary>
    ///     Gets the UTC instant at which the flow transitioned to <see cref="TrafficFlowStatus.Failed" />,
    ///     or <see langword="null" /> if the flow has not failed.
    /// </summary>
    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>
    ///     Transitions this flow to <see cref="TrafficFlowStatus.Failed" /> and records the failure time.
    ///     If the flow is already <see cref="TrafficFlowStatus.Failed" />, this method is a no-op.
    /// </summary>
    public void Fail()
    {
        if (Status == TrafficFlowStatus.Failed)
        {
            return;
        }

        Status = TrafficFlowStatus.Failed;
        FailedAt = DateTimeOffset.UtcNow;
    }
}
