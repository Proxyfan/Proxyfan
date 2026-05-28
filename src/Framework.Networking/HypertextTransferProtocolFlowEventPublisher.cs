using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Publishes <see cref="TrafficFlow" /> lifecycle events on the domain event bus. Extracted
///     from <see cref="HypertextTransferProtocolProxyHandler" /> to keep the handler under the
///     class-size analyzer rule.
/// </summary>
public sealed class HypertextTransferProtocolFlowEventPublisher
{
    private readonly IDomainEventBus _eventBus;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolFlowEventPublisher" />.
    /// </summary>
    /// <param name="eventBus">The bus to publish events on.</param>
    public HypertextTransferProtocolFlowEventPublisher(IDomainEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    ///     Publishes a <see cref="TrafficFlowCompleted" /> event for the supplied flow.
    /// </summary>
    /// <param name="flow">The flow that just completed.</param>
    public void PublishFlowCompleted(TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="TrafficFlowCreated" /> event for the supplied flow.
    /// </summary>
    /// <param name="flow">The flow that was just created.</param>
    public void PublishFlowCreated(TrafficFlow flow)
    {
        var createdEvent = new TrafficFlowCreated(flow.Id, DateTimeOffset.UtcNow);
        _eventBus.Publish(createdEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="RequestReceived" /> event for the supplied flow and request.
    /// </summary>
    /// <param name="flow">The flow the request belongs to.</param>
    /// <param name="request">The request received from the client.</param>
    public void PublishRequestReceived(TrafficFlow flow, HypertextTransferProtocolRequestData request)
    {
        var requestReceivedEvent = new RequestReceived(flow.Id, request, flow.ClientEndPoint, DateTimeOffset.UtcNow);
        _eventBus.Publish(requestReceivedEvent);
    }

    /// <summary>
    ///     Publishes a <see cref="ResponseReceived" /> event for the supplied flow and response.
    /// </summary>
    /// <param name="flow">The flow the response belongs to.</param>
    /// <param name="response">The response received from the upstream server.</param>
    public void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        _eventBus.Publish(responseReceivedEvent);
    }
}
