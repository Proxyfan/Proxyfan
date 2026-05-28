using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketUpgradeTunnel" /> verifying bidirectional relay,
///     close-frame capture, and flow lifecycle bookkeeping.
/// </summary>
public sealed class WebSocketUpgradeTunnelTests
{
    /// <summary>Verifies that messages from both directions are captured and the flow is marked closed.</summary>
    [Test]
    public async Task TunnelAsync_ClientAndServerMessagesAndClose_CapturedAndFlowClosed()
    {
        var clientFrames = ConcatBytes(BuildUnmaskedTextFrame("hi-from-client"), BuildCloseFrame());
        var serverFrames = ConcatBytes(BuildUnmaskedTextFrame("hi-from-server"), BuildCloseFrame());
        using var clientToProxy = new MemoryStream(clientFrames);
        using var proxyToClient = new MemoryStream();
        using var proxyToServer = new MemoryStream();
        using var serverToProxy = new MemoryStream(serverFrames);
        var clientStream = new DuplexStream(clientToProxy, proxyToClient);
        var upstreamStream = new DuplexStream(serverToProxy, proxyToServer);
        var trafficFlow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var webSocketFlow = new WebSocketFlow(trafficFlow);
        var tunnel = new WebSocketUpgradeTunnel(TimeProvider.System);

        await tunnel.TunnelAsync(clientStream, upstreamStream, webSocketFlow, CancellationToken.None);

        await Assert.That(webSocketFlow.IsClosed).IsTrue();
        await Assert.That(webSocketFlow.Messages.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(webSocketFlow.ClosedAt).IsNotNull();
    }

    /// <summary>Verifies that the flow is marked closed even when the tunnel ends without close frames.</summary>
    [Test]
    public async Task TunnelAsync_BothSidesClose_MarksWebSocketFlowClosed()
    {
        using var emptyClient = new MemoryStream();
        using var emptyServer = new MemoryStream();
        using var clientWrite = new MemoryStream();
        using var serverWrite = new MemoryStream();
        var clientStream = new DuplexStream(emptyClient, clientWrite);
        var upstreamStream = new DuplexStream(emptyServer, serverWrite);
        var trafficFlow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:1234", DateTimeOffset.UtcNow);
        var webSocketFlow = new WebSocketFlow(trafficFlow);
        var tunnel = new WebSocketUpgradeTunnel(TimeProvider.System);

        await tunnel.TunnelAsync(clientStream, upstreamStream, webSocketFlow, CancellationToken.None);

        await Assert.That(webSocketFlow.IsClosed).IsTrue();
    }

    private static byte[] BuildCloseFrame()
    {
        return new byte[] { 0x88, 0x00 };
    }

    private static byte[] BuildUnmaskedTextFrame(string text)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var bytes = new byte[2 + payload.Length];
        bytes[0] = 0x81;
        bytes[1] = (byte)payload.Length;
        Array.Copy(payload, 0, bytes, 2, payload.Length);
        return bytes;
    }

    private static byte[] ConcatBytes(byte[] first, byte[] second)
    {
        var combined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    private sealed class DuplexStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public DuplexStream(Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
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
            _writeStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _readStream.ReadAsync(buffer, cancellationToken);
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
            _writeStream.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _writeStream.WriteAsync(buffer, cancellationToken);
        }
    }
}
