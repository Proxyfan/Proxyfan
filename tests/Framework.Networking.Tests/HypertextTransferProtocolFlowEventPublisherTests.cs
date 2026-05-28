using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolFlowEventPublisher" />.
/// </summary>
public sealed class HypertextTransferProtocolFlowEventPublisherTests
{
    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolFlowEventPublisher.PublishFlowCreated" />
    ///     publishes a <see cref="TrafficFlowCreated" /> with the matching id.
    /// </summary>
    [Test]
    public async Task PublishFlowCreated_AnyFlow_PublishesTrafficFlowCreated()
    {
        var bus = new StubDomainEventBus();
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);

        publisher.PublishFlowCreated(flow);

        var events = bus.PublishedOf<TrafficFlowCreated>().ToArray();
        await Assert.That(events).HasCount(1);
        await Assert.That(events[0].TrafficFlowId).IsEqualTo(flow.Id);
    }

    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolFlowEventPublisher.PublishFlowCompleted" />
    ///     publishes a <see cref="TrafficFlowCompleted" /> with the flow's id and status.
    /// </summary>
    [Test]
    public async Task PublishFlowCompleted_AnyFlow_PublishesTrafficFlowCompleted()
    {
        var bus = new StubDomainEventBus();
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        flow.SetRequest(BuildRequest());
        flow.Complete();

        publisher.PublishFlowCompleted(flow);

        var events = bus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(events).HasCount(1);
        await Assert.That(events[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(events[0].Status).IsEqualTo(flow.Status);
    }

    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolFlowEventPublisher.PublishRequestReceived" />
    ///     publishes a <see cref="RequestReceived" /> carrying the request and the flow's client end point.
    /// </summary>
    [Test]
    public async Task PublishRequestReceived_AnyFlow_PublishesRequestReceived()
    {
        var bus = new StubDomainEventBus();
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var flow = new TrafficFlow(Guid.NewGuid(), "192.0.2.5:65000", DateTimeOffset.UtcNow);
        var request = BuildRequest();

        publisher.PublishRequestReceived(flow, request);

        var events = bus.PublishedOf<RequestReceived>().ToArray();
        await Assert.That(events).HasCount(1);
        await Assert.That(events[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(events[0].Request).IsSameReferenceAs(request);
        await Assert.That(events[0].ClientEndPoint).IsEqualTo("192.0.2.5:65000");
    }

    /// <summary>
    ///     Verifies <see cref="HypertextTransferProtocolFlowEventPublisher.PublishResponseReceived" />
    ///     publishes a <see cref="ResponseReceived" /> carrying the response.
    /// </summary>
    [Test]
    public async Task PublishResponseReceived_AnyFlow_PublishesResponseReceived()
    {
        var bus = new StubDomainEventBus();
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var response = BuildResponse();

        publisher.PublishResponseReceived(flow, response);

        var events = bus.PublishedOf<ResponseReceived>().ToArray();
        await Assert.That(events).HasCount(1);
        await Assert.That(events[0].TrafficFlowId).IsEqualTo(flow.Id);
        await Assert.That(events[0].Response).IsSameReferenceAs(response);
    }

    private static HypertextTransferProtocolRequestData BuildRequest()
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        return new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        });
    }

    private static HypertextTransferProtocolResponseData BuildResponse()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        return new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });
    }
}
