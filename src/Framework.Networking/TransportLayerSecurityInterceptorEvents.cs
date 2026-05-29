using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Domain-event publication helpers for <see cref="TransportLayerSecurityInterceptorHandler" />.
///     Extracted into a static class so the orchestrating handler remains under the analyzer-enforced
///     class size budget (ATXCS034) without sacrificing readability at each call site.
/// </summary>
public static class TransportLayerSecurityInterceptorEvents
{
    /// <summary>
    ///     Publishes a <see cref="TrafficFlowCompleted" /> event for the supplied flow.
    /// </summary>
    /// <param name="eventBus">The bus on which to publish the event.</param>
    /// <param name="flow">The traffic flow whose completion is being announced.</param>
    public static void PublishFlowCompleted(IDomainEventBus eventBus, TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        eventBus.Publish(completedEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="TrafficFlowCreated" /> event for the supplied flow.
    /// </summary>
    /// <param name="eventBus">The bus on which to publish the event.</param>
    /// <param name="flow">The newly created traffic flow.</param>
    public static void PublishFlowCreated(IDomainEventBus eventBus, TrafficFlow flow)
    {
        var createdEvent = new TrafficFlowCreated(flow.Id, DateTimeOffset.UtcNow);
        eventBus.Publish(createdEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="RequestReceived" /> event carrying the inbound request.
    /// </summary>
    /// <param name="eventBus">The bus on which to publish the event.</param>
    /// <param name="flow">The flow associated with the request.</param>
    /// <param name="request">The decoded request data.</param>
    public static void PublishRequestReceived(IDomainEventBus eventBus, TrafficFlow flow, HypertextTransferProtocolRequestData request)
    {
        var requestReceivedEvent = new RequestReceived(flow.Id, request, flow.ClientEndPoint, DateTimeOffset.UtcNow);
        eventBus.Publish(requestReceivedEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="ResponseReceived" /> event carrying the upstream response.
    /// </summary>
    /// <param name="eventBus">The bus on which to publish the event.</param>
    /// <param name="flow">The flow associated with the response.</param>
    /// <param name="response">The decoded response data.</param>
    public static void PublishResponseReceived(IDomainEventBus eventBus, TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        eventBus.Publish(responseReceivedEvent);
    }
}
