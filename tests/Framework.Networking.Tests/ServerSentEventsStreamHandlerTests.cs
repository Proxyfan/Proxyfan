using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsStreamHandler" />.
/// </summary>
public sealed class ServerSentEventsStreamHandlerTests
{
    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsStreamHandler.HandleAsync" /> writes the
    ///     response headers to the client, relays the upstream event bytes verbatim, captures
    ///     the parsed events into the SSE store, marks the flow complete, and publishes flow
    ///     lifecycle events on the bus.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamWritesEvents_RelaysAndCapturesAndCompletesFlow()
    {
        var bus = new StubDomainEventBus();
        var trafficStore = new StubTrafficStore();
        var sseStore = new ServerSentEventsStore(capacity: 4);
        var handler = new ServerSentEventsStreamHandler(
            bus,
            NullLogger.Instance,
            TimeProvider.System,
            trafficStore,
            sseStore);

        var sseBody = Encoding.UTF8.GetBytes("data: hello\n\ndata: world\n\n");
        using var upstream = new MemoryStream(sseBody);
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildStreamRequest(connection, upstream, prefetched: Array.Empty<byte>());
        request.Flow.SetRequest(request.EffectiveRequest);

        await handler.HandleAsync(request, CancellationToken.None);
        await connection.Transport.Output.CompleteAsync();
        var clientBytes = await connection.ReadAllOutputAsync();

        var clientText = Encoding.UTF8.GetString(clientBytes);
        await Assert.That(clientText).Contains("HTTP/1.1 200 OK");
        await Assert.That(clientText).Contains("data: hello");
        await Assert.That(clientText).Contains("data: world");

        var stored = sseStore.GetAll();
        await Assert.That(stored.Count).IsEqualTo(1);
        await Assert.That(stored[0].Events.Count).IsEqualTo(2);
        await Assert.That(stored[0].IsClosed).IsTrue();

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Id).IsEqualTo(request.Flow.Id);

        await Assert.That(bus.PublishedOf<ResponseReceived>().Count()).IsEqualTo(1);
        await Assert.That(bus.PublishedOf<TrafficFlowCompleted>().Count()).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an upstream <see cref="IOException" /> during the relay does not
    ///     propagate, the flow is still marked complete, and the SSE flow is closed.
    /// </summary>
    [Test]
    public async Task HandleAsync_UpstreamThrowsIoException_CompletesFlowAndSwallowsException()
    {
        var bus = new StubDomainEventBus();
        var trafficStore = new StubTrafficStore();
        var sseStore = new ServerSentEventsStore(capacity: 2);
        var handler = new ServerSentEventsStreamHandler(
            bus,
            NullLogger.Instance,
            TimeProvider.System,
            trafficStore,
            sseStore);

        using var upstream = new ThrowingStream(new IOException("simulated"));
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildStreamRequest(connection, upstream, prefetched: Array.Empty<byte>());
        request.Flow.SetRequest(request.EffectiveRequest);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        var stored = sseStore.GetAll();
        await Assert.That(stored.Count).IsEqualTo(1);
        await Assert.That(stored[0].IsClosed).IsTrue();
    }

    /// <summary>
    ///     Verifies that prefetched upstream bytes are replayed in the relay output before
    ///     the upstream stream content. This exercises the
    ///     <see cref="ServerSentEventsUpstreamStreams.Resolve" /> branch that wraps in
    ///     <see cref="PrefixedReadStream" />.
    /// </summary>
    [Test]
    public async Task HandleAsync_WithPrefetchedBytes_RelaysPrefixedThenUpstream()
    {
        var bus = new StubDomainEventBus();
        var trafficStore = new StubTrafficStore();
        var sseStore = new ServerSentEventsStore(capacity: 2);
        var handler = new ServerSentEventsStreamHandler(
            bus,
            NullLogger.Instance,
            TimeProvider.System,
            trafficStore,
            sseStore);

        var prefetched = Encoding.UTF8.GetBytes("data: pref\n\n");
        var remainingBytes = Encoding.UTF8.GetBytes("data: rest\n\n");
        using var upstream = new MemoryStream(remainingBytes);
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildStreamRequest(connection, upstream, prefetched);
        request.Flow.SetRequest(request.EffectiveRequest);

        await handler.HandleAsync(request, CancellationToken.None);
        await connection.Transport.Output.CompleteAsync();
        var clientBytes = await connection.ReadAllOutputAsync();
        var clientText = Encoding.UTF8.GetString(clientBytes);

        await Assert.That(clientText).Contains("data: pref");
        await Assert.That(clientText).Contains("data: rest");

        var stored = sseStore.GetAll();
        await Assert.That(stored.Count).IsEqualTo(1);
        await Assert.That(stored[0].Events.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that the handler still completes the flow when no SSE store is supplied
    ///     (null-store path).
    /// </summary>
    [Test]
    public async Task HandleAsync_NullSseStore_CompletesFlowWithoutThrowing()
    {
        var bus = new StubDomainEventBus();
        var trafficStore = new StubTrafficStore();
        var handler = new ServerSentEventsStreamHandler(
            bus,
            NullLogger.Instance,
            TimeProvider.System,
            trafficStore,
            serverSentEventsStore: null);

        var sseBody = Encoding.UTF8.GetBytes("data: x\n\n");
        using var upstream = new MemoryStream(sseBody);
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildStreamRequest(connection, upstream, prefetched: Array.Empty<byte>());
        request.Flow.SetRequest(request.EffectiveRequest);

        await handler.HandleAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
    }

    private static ServerSentEventsStreamRequest BuildStreamRequest(
        StubFullDuplexProxyConnection connection,
        Stream upstream,
        byte[] prefetched)
    {
        var requestHeaders = HeaderCollection.Empty.Add("Host", "example.com");
        var requestData = new HypertextTransferProtocolRequestData(new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = new Uri("http://example.com/events"),
            Version = "HTTP/1.1",
        });

        var responseHeaders = HeaderCollection.Empty.Add("Content-Type", "text/event-stream");
        var responseData = new HypertextTransferProtocolResponseData(new HypertextTransferProtocolResponseDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        });

        return new ServerSentEventsStreamRequest
        {
            Connection = connection,
            EffectiveRequest = requestData,
            Flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:5678", DateTimeOffset.UtcNow),
            ResponseHeaderBytes = Array.Empty<byte>(),
            ResponseHeaders = responseData,
            UpstreamPrefetched = prefetched,
            UpstreamStream = upstream,
        };
    }
}
