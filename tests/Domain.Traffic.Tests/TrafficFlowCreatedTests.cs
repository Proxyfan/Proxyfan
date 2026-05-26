using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for the domain event types in <c>Domain.Traffic.Events</c>.
/// </summary>
public sealed class TrafficFlowCreatedTests
{
    /// <summary>
    ///     Verifies that <see cref="Events.TrafficFlowCreated" /> stores all constructor parameters correctly.
    /// </summary>
    [Test]
    public async Task TrafficFlowCreated_Constructor_StoresAllValues()
    {
        var flowId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var domainEvent = new Events.TrafficFlowCreated(flowId, timestamp);

        await Assert.That(domainEvent.TrafficFlowId).IsEqualTo(flowId);
        await Assert.That(domainEvent.Timestamp).IsEqualTo(timestamp);
    }

    /// <summary>
    ///     Verifies that <see cref="Events.TrafficFlowCompleted" /> stores all constructor parameters correctly.
    /// </summary>
    [Test]
    public async Task TrafficFlowCompleted_Constructor_StoresAllValues()
    {
        var flowId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var domainEvent = new Events.TrafficFlowCompleted(flowId, TrafficFlowStatus.Complete, timestamp);

        await Assert.That(domainEvent.TrafficFlowId).IsEqualTo(flowId);
        await Assert.That(domainEvent.Status).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(domainEvent.Timestamp).IsEqualTo(timestamp);
    }

    /// <summary>
    ///     Verifies that <see cref="Events.RequestReceived" /> stores all constructor parameters correctly.
    /// </summary>
    [Test]
    public async Task RequestReceived_Constructor_StoresAllValues()
    {
        var flowId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);

        var domainEvent = new Events.RequestReceived(flowId, request, "127.0.0.1:9000", timestamp);

        await Assert.That(domainEvent.TrafficFlowId).IsEqualTo(flowId);
        await Assert.That(domainEvent.Request).IsSameReferenceAs(request);
        await Assert.That(domainEvent.ClientEndPoint).IsEqualTo("127.0.0.1:9000");
        await Assert.That(domainEvent.Timestamp).IsEqualTo(timestamp);
    }

    /// <summary>
    ///     Verifies that <see cref="Events.ResponseReceived" /> stores all constructor parameters correctly.
    /// </summary>
    [Test]
    public async Task ResponseReceived_Constructor_StoresAllValues()
    {
        var flowId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        byte[] body = [1, 2, 3];
        var headers = HeaderCollection.Empty.Add("Content-Length", "3");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = body,
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);

        var domainEvent = new Events.ResponseReceived(flowId, response, timestamp);

        await Assert.That(domainEvent.TrafficFlowId).IsEqualTo(flowId);
        await Assert.That(domainEvent.Response).IsSameReferenceAs(response);
        await Assert.That(domainEvent.Timestamp).IsEqualTo(timestamp);
    }
}
