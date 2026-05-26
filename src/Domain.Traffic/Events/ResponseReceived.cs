using System;

namespace Proxyfan.Domain.Traffic.Events;

/// <summary>
///     Published when an HTTP response has been captured for a traffic flow.
/// </summary>
public sealed class ResponseReceived : IDomainEvent
{
    /// <summary>
    ///     Gets the captured HTTP response.
    /// </summary>
    public HypertextTransferProtocolResponseData Response { get; }

    /// <summary>
    ///     Gets the UTC instant at which the response was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Gets the traffic flow identifier.
    /// </summary>
    public Guid TrafficFlowId { get; }

    /// <summary>
    ///     Initializes a new <see cref="ResponseReceived" /> instance.
    /// </summary>
    /// <param name="trafficFlowId">
    ///     The traffic flow identifier.
    /// </param>
    /// <param name="response">
    ///     The captured HTTP response.
    /// </param>
    /// <param name="timestamp">
    ///     The UTC instant at which the response was captured.
    /// </param>
    public ResponseReceived(Guid trafficFlowId, HypertextTransferProtocolResponseData response, DateTimeOffset timestamp)
    {
        Response = response;
        Timestamp = timestamp;
        TrafficFlowId = trafficFlowId;
    }
}