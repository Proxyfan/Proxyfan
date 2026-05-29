using Microsoft.Extensions.Logging.Abstractions;
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
///     Tests for <see cref="TransportLayerSecurityInterceptedVersion2Dispatch" />.
/// </summary>
public sealed class TransportLayerSecurityInterceptedVersion2DispatchTests
{
    /// <summary>
    ///     Verifies that <see cref="TransportLayerSecurityInterceptedVersion2Dispatch.RunAsync" />
    ///     builds an orchestrator that captures an HTTP/2 request/response pair when both
    ///     streams receive a HEADERS-END_STREAM frame.
    /// </summary>
    [Test]
    public async Task RunAsync_BasicExchange_CapturesFlow()
    {
        var bus = new StubDomainEventBus();
        var store = new StubTrafficStore();
        var clientToProxyPipe = new Pipe();
        var proxyToUpstreamPipe = new Pipe();
        var upstreamToProxyPipe = new Pipe();
        var proxyToClientPipe = new Pipe();
        using var clientSide = BuildPairedStream(clientToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToClientPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamSide = BuildPairedStream(upstreamToProxyPipe.Writer.AsStream(leaveOpen: true), proxyToUpstreamPipe.Reader.AsStream(leaveOpen: true));
        using var clientFacingStream = BuildPairedStream(proxyToClientPipe.Writer.AsStream(leaveOpen: true), clientToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var upstreamFacingStream = BuildPairedStream(proxyToUpstreamPipe.Writer.AsStream(leaveOpen: true), upstreamToProxyPipe.Reader.AsStream(leaveOpen: true));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var connection = new StubFullDuplexProxyConnection();
        var dispatchRequest = new TransportLayerSecurityInterceptedVersion2DispatchRequest
        {
            ClientSecureStream = clientFacingStream,
            Connection = connection,
            EventBus = bus,
            ServerSecureStream = upstreamFacingStream,
            TrafficStore = store,
        };

        var clientEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var upstreamEncoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var requestHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":method", "GET"),
            new(":scheme", "https"),
            new(":authority", "example.com"),
            new(":path", "/test"),
        };
        var responseHeaders = new List<HypertextTransferProtocolVersion2HpackHeaderField>
        {
            new(":status", "200"),
        };

        var runTask = TransportLayerSecurityInterceptedVersion2Dispatch.RunAsync(dispatchRequest, cancellation.Token);

        var requestFrame = BuildFrame(clientEncoder.Encode(requestHeaders), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1);
        await clientSide.WriteAsync(requestFrame, cancellation.Token);
        await clientSide.FlushAsync(cancellation.Token);

        var responseFrame = BuildFrame(upstreamEncoder.Encode(responseHeaders), HypertextTransferProtocolVersion2FrameType.Headers, HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge, 1);
        await upstreamSide.WriteAsync(responseFrame, cancellation.Token);
        await upstreamSide.FlushAsync(cancellation.Token);

        await WaitForBytesAsync(upstreamSide, cancellation.Token);
        await WaitForBytesAsync(clientSide, cancellation.Token);

        await clientToProxyPipe.Writer.CompleteAsync();
        await upstreamToProxyPipe.Writer.CompleteAsync();
        await runTask;

        var completed = bus.PublishedOf<TrafficFlowCompleted>().ToArray();
        await Assert.That(completed.Length).IsEqualTo(1);
        await Assert.That(store.AddedFlows.Count).IsEqualTo(1);
    }

    private static byte[] BuildFrame(byte[] payload, HypertextTransferProtocolVersion2FrameType type, HypertextTransferProtocolVersion2FrameFlag flags, uint streamId)
    {
        var totalLength = HypertextTransferProtocolVersion2FrameParser.HeaderLength + payload.Length;
        var buffer = new byte[totalLength];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            Flags = flags,
            PayloadLength = payload.Length,
            StreamIdentifier = streamId,
            Type = type,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        return buffer;
    }

    private static PairedDuplexStream BuildPairedStream(Stream writeSide, Stream readSide)
    {
        var paired = new PairedDuplexStream(writeSide, readSide);
        return paired;
    }

    private static async Task WaitForBytesAsync(PairedDuplexStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[HypertextTransferProtocolVersion2FrameParser.HeaderLength];
        var totalRead = 0;
        while (totalRead < buffer.Length && !cancellationToken.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }
            totalRead += read;
        }
    }

    private sealed class PairedDuplexStream : Stream, IDisposable
    {
        private readonly Stream _readSide;
        private readonly Stream _writeSide;

        public PairedDuplexStream(Stream writeSide, Stream readSide)
        {
            _writeSide = writeSide;
            _readSide = readSide;
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
