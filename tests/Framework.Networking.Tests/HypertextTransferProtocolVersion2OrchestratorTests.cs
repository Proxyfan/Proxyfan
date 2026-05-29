using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     End-to-end tests for <see cref="HypertextTransferProtocolVersion2Orchestrator" /> that
///     drive frames through in-memory duplex stream pairs and verify that the orchestrator
///     captures HTTP/2 request/response pairs into the traffic store and forwards every
///     frame byte-for-byte to the destination side.
/// </summary>
public sealed class HypertextTransferProtocolVersion2OrchestratorTests
{
    /// <summary>
    ///     Drives a single HEADERS-END_STREAM request and a single HEADERS-END_STREAM response
    ///     through the orchestrator and verifies that a captured <see cref="TrafficFlow" />
    ///     with both the request and response lands in the traffic store, and that the bytes
    ///     are forwarded verbatim to the opposite side.
    /// </summary>
    [Test]
    public async Task RunAsync_SingleStreamHeadersOnly_CapturesAndForwardsBothSides()
    {
        var (orchestrator, bus, store) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var upstreamEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/hello"),
            new("accept", "application/json"),
        };
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
            new("content-type", "application/json"),
            new("content-length", "0"),
        };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50000", cancellation.Token);

        // Client sends request HEADERS with END_STREAM + END_HEADERS
        var requestPayload = clientEncoder.Encode(requestHeaders);
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, requestPayload);

        // Wait for the upstream side to receive the frame
        var receivedFromOrchestrator = await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);
        await Assert.That(receivedFromOrchestrator).IsNotNull();

        // Upstream sends response HEADERS with END_STREAM + END_HEADERS
        var responsePayload = upstreamEncoder.Encode(responseHeaders);
        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, responsePayload);

        // Wait for the client side to receive the response
        var receivedOnClient = await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);
        await Assert.That(receivedOnClient).IsNotNull();

        // Close both directions to make the pumps exit
        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        await Assert.That(store.AddedFlows.Count).IsEqualTo(1);
        var captured = store.AddedFlows[0];
        await Assert.That(captured.Request).IsNotNull();
        await Assert.That(captured.Request!.Method).IsEqualTo("GET");
        await Assert.That(captured.Request.RequestUri.ToString()).IsEqualTo("https://example.com/hello");
        await Assert.That(captured.Request.Version).IsEqualTo("HTTP/2");
        await Assert.That(captured.Response).IsNotNull();
        await Assert.That(captured.Response!.StatusCode).IsEqualTo(200);
        await Assert.That(captured.Response.Version).IsEqualTo("HTTP/2");
        await Assert.That(captured.Status).IsEqualTo(TrafficFlowStatus.Complete);
        await Assert.That(bus.PublishedOf<TrafficFlowCreated>().Count()).IsEqualTo(1);
        await Assert.That(bus.PublishedOf<TrafficFlowCompleted>().Count()).IsEqualTo(1);
    }

    /// <summary>
    ///     Drives a request stream with HEADERS + DATA (no END_STREAM on HEADERS) and a
    ///     response with HEADERS + DATA + END_STREAM and verifies the captured flow carries
    ///     both bodies.
    /// </summary>
    [Test]
    public async Task RunAsync_RequestAndResponseDataFrames_CapturesBodiesOnBothSides()
    {
        var (orchestrator, _, store) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var upstreamEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "POST"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/echo"),
        };
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
            new("content-type", "text/plain"),
        };
        var requestBody = new byte[] { 1, 2, 3, 4 };
        var responseBody = new byte[] { 5, 6 };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50001", cancellation.Token);

        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders, 1, clientEncoder.Encode(requestHeaders));
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Data, HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, requestBody);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);

        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders, 1, upstreamEncoder.Encode(responseHeaders));
        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Data, HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, responseBody);
        await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);
        await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);

        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        await Assert.That(store.AddedFlows.Count).IsEqualTo(1);
        var captured = store.AddedFlows[0];
        await Assert.That(captured.Request!.Body.ToArray()).IsEquivalentTo(requestBody);
        await Assert.That(captured.Response!.Body.ToArray()).IsEquivalentTo(responseBody);
    }

    /// <summary>
    ///     Drives a request whose stream is reset by the client mid-flight (RST_STREAM after
    ///     HEADERS) and verifies the orchestrator marks the flow failed instead of completed.
    /// </summary>
    [Test]
    public async Task RunAsync_RequestResetByClient_FailsFlow()
    {
        var (orchestrator, bus, _) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/cancel"),
        };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50002", cancellation.Token);
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders, 1, clientEncoder.Encode(requestHeaders));
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.ResetStream, HypertextTransferProtocolVersion2FrameFlag.None, 1, new byte[] { 0, 0, 0, 8 });
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);

        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        var completed = bus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(completed.Length).IsEqualTo(1);
        await Assert.That(completed[0].Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    /// <summary>
    ///     Drives request HEADERS split across HEADERS (no END_HEADERS) + CONTINUATION
    ///     (END_HEADERS, END_STREAM placed on the HEADERS frame) and verifies the orchestrator
    ///     assembles them into a single request and forwards both frames.
    /// </summary>
    [Test]
    public async Task RunAsync_RequestHeadersSplitAcrossContinuation_AssemblesAndCaptures()
    {
        var (orchestrator, _, store) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var upstreamEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/split"),
        };
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
        };
        var encodedRequest = clientEncoder.Encode(requestHeaders);
        var firstHalf = encodedRequest.AsSpan(0, encodedRequest.Length / 2).ToArray();
        var secondHalf = encodedRequest.AsSpan(encodedRequest.Length / 2).ToArray();

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50003", cancellation.Token);

        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, firstHalf);
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Continuation, HypertextTransferProtocolVersion2FrameFlag.EndHeaders, 1, secondHalf);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);

        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, upstreamEncoder.Encode(responseHeaders));
        await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);

        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        await Assert.That(store.AddedFlows.Count).IsEqualTo(1);
        var captured = store.AddedFlows[0];
        await Assert.That(captured.Request).IsNotNull();
        await Assert.That(captured.Request!.RequestUri.ToString()).IsEqualTo("https://example.com/split");
    }

    /// <summary>
    ///     Verifies that DATA frames with the PADDED flag are correctly parsed (padding
    ///     stripped) before being appended to the request/response bodies.
    /// </summary>
    [Test]
    public async Task RunAsync_PaddedDataFrame_StripsPadding()
    {
        var (orchestrator, _, store) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var upstreamEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "POST"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/padded"),
        };
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
        };

        // PADDED DATA frame: [pad-length=2][payload=A,B][padding=X,X]
        var paddedRequestPayload = new byte[] { 2, (byte)'A', (byte)'B', 0xFF, 0xFF };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50004", cancellation.Token);
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders, 1, clientEncoder.Encode(requestHeaders));
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Data, HypertextTransferProtocolVersion2FrameFlag.Padded | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, paddedRequestPayload);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);

        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, upstreamEncoder.Encode(responseHeaders));
        await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);

        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        await Assert.That(store.AddedFlows.Count).IsEqualTo(1);
        var captured = store.AddedFlows[0];
        await Assert.That(captured.Request!.Body.ToArray()).IsEquivalentTo(new byte[] { (byte)'A', (byte)'B' });
    }

    /// <summary>
    ///     Verifies that GOAWAY frames pass through verbatim without disrupting the relay.
    /// </summary>
    [Test]
    public async Task RunAsync_GoAwayFrame_PassesThroughVerbatim()
    {
        var (orchestrator, _, _) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var goAwayPayload = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50005", cancellation.Token);
        WriteFrame(upstreamSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.GoAway, HypertextTransferProtocolVersion2FrameFlag.None, 0, goAwayPayload);
        var received = await ReadOneFrameFromAsync(clientSide.InputFromOrchestrator(), cancellation.Token);

        await Assert.That(received).IsNotNull();
        await Assert.That(received!.Header.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.GoAway);

        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;
    }

    /// <summary>
    ///     Verifies that an unfinished flow (HEADERS sent but no END_STREAM, then the
    ///     connection closes) is marked failed by <c>FailUnfinishedFlows</c>.
    /// </summary>
    [Test]
    public async Task RunAsync_UnfinishedFlowAtShutdown_IsFailed()
    {
        var (orchestrator, bus, _) = BuildOrchestrator();
        var clientToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToUpstreamPipe = new System.IO.Pipelines.Pipe();
        var upstreamToProxyPipe = new System.IO.Pipelines.Pipe();
        var proxyToClientPipe = new System.IO.Pipelines.Pipe();
        using var clientSide = new PairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = new PairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = new PairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = new PairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/never-completes"),
        };

        var runTask = orchestrator.RunAsync(clientFacingStream, upstreamFacingStream, "127.0.0.1:50006", cancellation.Token);
        // HEADERS with END_HEADERS + END_STREAM creates the flow and marks request side ended
        WriteFrame(clientSide.OutputForOrchestrator(), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1, clientEncoder.Encode(requestHeaders));
        await ReadOneFrameFromAsync(upstreamSide.InputFromOrchestrator(), cancellation.Token);

        // Close both pipes without ever sending a response — FailUnfinishedFlows should kick in
        await clientSide.OutputForOrchestrator().CompleteAsync();
        await upstreamSide.OutputForOrchestrator().CompleteAsync();
        await runTask;

        var completed = bus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(completed.Length).IsEqualTo(1);
        await Assert.That(completed[0].Status).IsEqualTo(TrafficFlowStatus.Failed);
    }

    private static (HypertextTransferProtocolVersion2Orchestrator orchestrator, StubDomainEventBus bus, StubTrafficStore store) BuildOrchestrator()
    {
        var bus = new StubDomainEventBus();
        var store = new StubTrafficStore();
        var publisher = new HypertextTransferProtocolFlowEventPublisher(bus);
        var dependencies = new HypertextTransferProtocolVersion2OrchestratorDependencies
        {
            FlowEventPublisher = publisher,
            TrafficStore = store,
        };
        var orchestrator = new HypertextTransferProtocolVersion2Orchestrator(dependencies);
        return (orchestrator, bus, store);
    }

    private static void WriteFrame(
        PipeWriter writer,
        HypertextTransferProtocolVersion2FrameType type,
        HypertextTransferProtocolVersion2FrameFlag flags,
        uint streamIdentifier,
        byte[] payload)
    {
        var totalLength = HypertextTransferProtocolVersion2FrameParser.HeaderLength + payload.Length;
        var buffer = new byte[totalLength];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            Flags = flags,
            PayloadLength = payload.Length,
            StreamIdentifier = streamIdentifier,
            Type = type,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        var span = writer.GetSpan(buffer.Length);
        buffer.AsSpan().CopyTo(span);
        writer.Advance(buffer.Length);
        var flushTask = writer.FlushAsync();
        flushTask.AsTask().GetAwaiter().GetResult();
    }

    private static async Task<HypertextTransferProtocolVersion2Frame?> ReadOneFrameFromAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var frame = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(reader, cancellationToken);
        return frame;
    }

    private sealed class PairedStream : Stream, IDisposable
    {
        private readonly Stream _readSide;
        private readonly Stream _writeSide;
        private readonly PipeReader _orchestratorReader;
        private readonly PipeWriter _orchestratorWriter;

        public PairedStream(Stream writeSide, Stream readSide)
        {
            _writeSide = writeSide;
            _readSide = readSide;
            _orchestratorReader = PipeReader.Create(readSide, new StreamPipeReaderOptions(leaveOpen: true));
            _orchestratorWriter = PipeWriter.Create(writeSide, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _writeSide.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeSide.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readSide.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _readSide.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeSide.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _writeSide.WriteAsync(buffer, cancellationToken);
        }

        public PipeWriter OutputForOrchestrator()
        {
            return _orchestratorWriter;
        }

        public PipeReader InputFromOrchestrator()
        {
            return _orchestratorReader;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _writeSide.Dispose();
                _readSide.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
