using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Unit tests for <see cref="TransportLayerSecurityResponsePhaseWriter" /> covering the
///     try/finally contract that guarantees the captured traffic flow is recorded and the
///     <see cref="TrafficFlowCompleted" /> event is published even when the response write
///     throws (which happens when the client closes the TLS connection immediately after
///     reading a <c>Connection: close</c> response).
/// </summary>
public sealed class TransportLayerSecurityResponsePhaseWriterTests
{
    /// <summary>
    ///     Verifies that a successful write records the flow into the traffic store and
    ///     publishes a <see cref="TrafficFlowCompleted" /> event carrying the flow id.
    /// </summary>
    [Test]
    public async Task WriteAndPublishBookkeepingAsync_SuccessfulWrite_RecordsFlowAndPublishesEvent()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();
        var exchange = BuildResponseExchange();
        var pipe = new Pipe();
        var request = new TransportLayerSecurityResponsePhaseWriteRequest
        {
            Exchange = exchange,
            EventBus = eventBus,
            Flow = flow,
            TrafficStore = trafficStore,
            Writer = pipe.Writer,
        };

        await TransportLayerSecurityResponsePhaseWriter.WriteAndPublishBookkeepingAsync(request, CancellationToken.None);

        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Id).IsEqualTo(flow.Id);
        var published = eventBus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(published.Length).IsEqualTo(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
    }

    /// <summary>
    ///     Verifies that when the underlying writer throws (simulating a client that closed the
    ///     TLS connection mid-write), the flow is still recorded and the
    ///     <see cref="TrafficFlowCompleted" /> event is still published. The original exception
    ///     propagates to the caller.
    /// </summary>
    [Test]
    public async Task WriteAndPublishBookkeepingAsync_WriterThrows_StillRecordsFlowAndPublishesEvent()
    {
        var trafficStore = new StubTrafficStore();
        var eventBus = new StubDomainEventBus();
        var flow = CreateFlow();
        var exchange = BuildResponseExchange();
        var writer = new ThrowingPipeWriter(new IOException("client closed"));
        var request = new TransportLayerSecurityResponsePhaseWriteRequest
        {
            Exchange = exchange,
            EventBus = eventBus,
            Flow = flow,
            TrafficStore = trafficStore,
            Writer = writer,
        };

        IOException? caughtException = null;
        try
        {
            await TransportLayerSecurityResponsePhaseWriter.WriteAndPublishBookkeepingAsync(request, CancellationToken.None);
        }
        catch (IOException ex)
        {
            caughtException = ex;
        }

        await Assert.That(caughtException).IsNotNull();
        await Assert.That(caughtException!.Message).IsEqualTo("client closed");
        await Assert.That(trafficStore.AddedFlows.Count).IsEqualTo(1);
        await Assert.That(trafficStore.AddedFlows[0].Id).IsEqualTo(flow.Id);
        var published = eventBus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(published.Length).IsEqualTo(1);
        await Assert.That(published[0].TrafficFlowId).IsEqualTo(flow.Id);
    }

    private static HypertextTransferProtocolProxyResponseExchange BuildResponseExchange()
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "5");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Encoding.ASCII.GetBytes("hello"),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        var headerBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\n\r\n");
        var body = Encoding.ASCII.GetBytes("hello");
        return new HypertextTransferProtocolProxyResponseExchange(body, headerBytes, response);
    }

    private static TrafficFlow CreateFlow()
    {
        return new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
    }

    private sealed class ThrowingPipeWriter : PipeWriter
    {
        private readonly Exception _exception;
        private readonly byte[] _scratchBuffer;

        public ThrowingPipeWriter(Exception exception)
        {
            _exception = exception;
            _scratchBuffer = new byte[4096];
        }

        public override void Advance(int bytes)
        {
        }

        public override void CancelPendingFlush()
        {
        }

        public override void Complete(Exception? exception = null)
        {
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            return _scratchBuffer.AsMemory();
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            return _scratchBuffer.AsSpan();
        }

        public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}
