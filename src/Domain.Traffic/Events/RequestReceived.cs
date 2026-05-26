using System;

namespace Proxyfan.Domain.Traffic.Events;

/// <summary>
///     Published when an HTTP request has been captured for a traffic flow.
/// </summary>
public sealed class RequestReceived : IDomainEvent
{
    /// <summary>
    ///     Gets the client endpoint associated with the flow.
    /// </summary>
    public string ClientEndPoint { get; }

    /// <summary>
    ///     Gets the captured HTTP request.
    /// </summary>
    public HypertextTransferProtocolRequestData Request { get; }

    /// <summary>
    ///     Gets the UTC instant at which the request was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Gets the traffic flow identifier.
    /// </summary>
    public Guid TrafficFlowId { get; }

    /// <summary>
    ///     Initializes a new <see cref="RequestReceived" /> instance.
    /// </summary>
    /// <param name="trafficFlowId">
    ///     The traffic flow identifier.
    /// </param>
    /// <param name="request">
    ///     The captured HTTP request.
    /// </param>
    /// <param name="clientEndPoint">
    ///     The client endpoint associated with the flow.
    /// </param>
    /// <param name="timestamp">
    ///     The UTC instant at which the request was captured.
    /// </param>
    public RequestReceived(Guid trafficFlowId, HypertextTransferProtocolRequestData request, string clientEndPoint, DateTimeOffset timestamp)
    {
        ClientEndPoint = clientEndPoint;
        Request = request;
        Timestamp = timestamp;
        TrafficFlowId = trafficFlowId;
    }
}