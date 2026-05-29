using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="TransportLayerSecurityInterceptorEvents" /> covering the four
///     publication helpers that the TLS interceptor handler uses to broadcast traffic flow
///     lifecycle events on the in-process domain event bus.
/// </summary>
public sealed class TransportLayerSecurityInterceptorEventsTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorEvents.PublishFlowCompleted" />
    ///     publishes a <see cref="TrafficFlowCompleted" /> carrying the flow's id and status.
    /// </summary>
    [Test]
    public async Task PublishFlowCompleted_FailedFlow_PublishesEventWithStatus()
    {
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();
        flow.SetRequest(BuildRequest());
        flow.Fail();

        TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(eventBus, flow);

        var published = eventBus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(published).HasCount(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(published[0].Status).IsEqualTo(flow.Status);
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorEvents.PublishFlowCreated" />
    ///     publishes a <see cref="TrafficFlowCreated" /> carrying the flow's id.
    /// </summary>
    [Test]
    public async Task PublishFlowCreated_NewFlow_PublishesEventWithFlowId()
    {
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();

        TransportLayerSecurityInterceptorEvents.PublishFlowCreated(eventBus, flow);

        var published = eventBus.PublishedOf<TrafficFlowCreated>().ToArray();
        await Assert.That(published).HasCount(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorEvents.PublishRequestReceived" />
    ///     publishes a <see cref="RequestReceived" /> carrying the supplied request and the flow's id.
    /// </summary>
    [Test]
    public async Task PublishRequestReceived_GivenRequest_PublishesEventWithRequest()
    {
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();
        var request = BuildRequest();

        TransportLayerSecurityInterceptorEvents.PublishRequestReceived(eventBus, flow, request);

        var published = eventBus.PublishedOf<RequestReceived>().ToArray();
        await Assert.That(published).HasCount(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(published[0].Request).IsSameReferenceAs(request);
        await Assert.That(published[0].ClientEndPoint).IsEqualTo(flow.ClientEndPoint);
    }

    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptorEvents.PublishResponseReceived" />
    ///     publishes a <see cref="ResponseReceived" /> carrying the supplied response and the flow's id.
    /// </summary>
    [Test]
    public async Task PublishResponseReceived_GivenResponse_PublishesEventWithResponse()
    {
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();
        var response = BuildResponse();

        TransportLayerSecurityInterceptorEvents.PublishResponseReceived(eventBus, flow, response);

        var published = eventBus.PublishedOf<ResponseReceived>().ToArray();
        await Assert.That(published).HasCount(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(published[0].Response).IsSameReferenceAs(response);
    }

    private static HypertextTransferProtocolRequestData BuildRequest()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolRequestData(requestParameters);
    }

    private static HypertextTransferProtocolResponseData BuildResponse()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        return new HypertextTransferProtocolResponseData(responseParameters);
    }

    private static TrafficFlow CreateFlow()
    {
        return new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
    }
}
