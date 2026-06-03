using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketRelay" /> verifying that frames are forwarded byte-for-byte
///     to the destination stream and reassembled into <see cref="WebSocketMessage" /> callbacks.
/// </summary>
public sealed class WebSocketRelayTests
{
    /// <summary>
    ///     Verifies that a single unmasked text frame from the server is forwarded verbatim and
    ///     surfaced via the message callback.
    /// </summary>
    [Test]
    public async Task RelayAsync_SingleUnmaskedTextFrame_ForwardsAndCaptures()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var frame = BuildUnmaskedTextFrame("hello");
        using var source = new MemoryStream(frame);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(captured[0].Opcode).IsEqualTo(WebSocketOpcode.Text);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("hello");
        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
    }

    /// <summary>
    ///     Verifies that two back-to-back frames in a single buffer are both captured.
    /// </summary>
    [Test]
    public async Task RelayAsync_TwoConcatenatedFrames_CapturesBoth()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var first = BuildUnmaskedTextFrame("one");
        var second = BuildUnmaskedTextFrame("two");
        var combined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        using var source = new MemoryStream(combined);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("one");
        await Assert.That(Encoding.UTF8.GetString(captured[1].Payload.Span)).IsEqualTo("two");
    }

    /// <summary>
    ///     Verifies that a Close frame stops the relay loop after capturing it.
    /// </summary>
    [Test]
    public async Task RelayAsync_CloseFrame_StopsRelaying()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var close = BuildUnmaskedControlFrame(WebSocketOpcode.Close);
        var trailing = BuildUnmaskedTextFrame("ignored");
        var combined = new byte[close.Length + trailing.Length];
        Array.Copy(close, 0, combined, 0, close.Length);
        Array.Copy(trailing, 0, combined, close.Length, trailing.Length);
        using var source = new MemoryStream(combined);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured[0].Opcode).IsEqualTo(WebSocketOpcode.Close);
    }

    /// <summary>
    ///     Verifies that a masked client-to-server frame is forwarded verbatim (mask byte preserved)
    ///     while the captured message contains the unmasked payload.
    /// </summary>
    [Test]
    public async Task RelayAsync_MaskedClientFrame_ForwardsVerbatimAndUnmasksForCapture()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Outbound, captured.Add, TimeProvider.System);
        var frame = BuildMaskedTextFrame("hi", new byte[] { 0xAA, 0xBB, 0xCC, 0xDD });
        using var source = new MemoryStream(frame);
        using var destination = new MemoryStream();

        await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("hi");
        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
    }

    /// <summary>
    ///     Verifies that a fragmented two-frame message is reassembled into a single captured message.
    /// </summary>
    [Test]
    public async Task RelayAsync_FragmentedMessage_ReassemblesIntoSingleMessage()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var first = BuildUnmaskedTextFragment("foo", isFinal: false, isContinuation: false);
        var second = BuildUnmaskedTextFragment("bar", isFinal: true, isContinuation: true);
        var combined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        using var source = new MemoryStream(combined);
        using var destination = new MemoryStream();

        await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("foobar");
    }

    /// <summary>
    ///     Verifies that an empty source stream returns zero messages.
    /// </summary>
    [Test]
    public async Task RelayAsync_EmptySource_ReturnsZero()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        using var source = new MemoryStream(Array.Empty<byte>());
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(captured.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A partial frame (only a single byte arrives before the rest, then the source closes)
    ///     is forwarded byte-for-byte but produces no captured messages. Exercises the
    ///     incomplete-frame / no-progress branches in <c>DrainCompletedFrames</c>.
    /// </summary>
    [Test]
    public async Task RelayAsync_PartialFrameOnly_ForwardsButCapturesNothing()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var partialFrameSingleByte = new byte[] { 0x81 };
        using var source = new MemoryStream(partialFrameSingleByte);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(captured.Count).IsEqualTo(0);
        await Assert.That(destination.ToArray()).IsEquivalentTo(partialFrameSingleByte);
    }

    /// <summary>
    ///     Frames split byte-by-byte across many reads are reassembled correctly; verifies that
    ///     the accumulator can stitch fragments together across many reads without losing or
    ///     duplicating bytes (exercises the consume-from-head / read-into-tail pump).
    /// </summary>
    [Test]
    public async Task RelayAsync_DrippedOneByteAtATime_ReassemblesAndForwardsAll()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        var first = BuildUnmaskedTextFrame("alpha");
        var second = BuildUnmaskedTextFrame("bravo");
        var combined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        using var source = new SingleByteReadStream(combined);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("alpha");
        await Assert.That(Encoding.UTF8.GetString(captured[1].Payload.Span)).IsEqualTo("bravo");
        await Assert.That(destination.ToArray()).IsEquivalentTo(combined);
    }

    /// <summary>
    ///     A long stream of small frames drains the accumulator continually and forces the
    ///     internal compaction path to kick in repeatedly; verifies no bytes are lost or
    ///     duplicated under sustained throughput.
    /// </summary>
    [Test]
    public async Task RelayAsync_ManySmallFramesAcrossSmallReads_PreservesEveryFrame()
    {
        var captured = new List<WebSocketMessage>();
        var relay = new WebSocketRelay(WebSocketDirection.Inbound, captured.Add, TimeProvider.System);
        const int frameCount = 1024;
        var pieces = new List<byte[]>(frameCount);
        var totalLength = 0;
        for (var index = 0; index < frameCount; index++)
        {
            var frame = BuildUnmaskedTextFrame($"f{index}");
            pieces.Add(frame);
            totalLength += frame.Length;
        }

        var combined = new byte[totalLength];
        var offset = 0;
        foreach (var piece in pieces)
        {
            Array.Copy(piece, 0, combined, offset, piece.Length);
            offset += piece.Length;
        }

        using var source = new ChunkedReadStream(combined, chunkSize: 7);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(frameCount);
        await Assert.That(captured.Count).IsEqualTo(frameCount);
        await Assert.That(destination.ToArray()).IsEquivalentTo(combined);
        await Assert.That(Encoding.UTF8.GetString(captured[0].Payload.Span)).IsEqualTo("f0");
        await Assert.That(Encoding.UTF8.GetString(captured[frameCount - 1].Payload.Span)).IsEqualTo($"f{frameCount - 1}");
    }

    private sealed class SingleByteReadStream : Stream
    {
        private readonly byte[] _data;
        private int _position;

        public SingleByteReadStream(byte[] data)
        {
            _data = data;
            _position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _data.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _data.Length)
            {
                return 0;
            }

            buffer[offset] = _data[_position];
            _position++;
            return 1;
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
            throw new NotSupportedException();
        }
    }

    private sealed class ChunkedReadStream : Stream
    {
        private readonly int _chunkSize;
        private readonly byte[] _data;
        private int _position;

        public ChunkedReadStream(byte[] data, int chunkSize)
        {
            _data = data;
            _chunkSize = chunkSize;
            _position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _data.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _data.Length)
            {
                return 0;
            }

            var remaining = _data.Length - _position;
            var toCopy = Math.Min(Math.Min(_chunkSize, count), remaining);
            Array.Copy(_data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
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
            throw new NotSupportedException();
        }
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

    private static byte[] BuildUnmaskedTextFragment(string text, bool isFinal, bool isContinuation)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var bytes = new byte[2 + payload.Length];
        byte finBit = isFinal ? (byte)0x80 : (byte)0x00;
        byte opcode = isContinuation ? (byte)0x00 : (byte)0x01;
        bytes[0] = (byte)(finBit | opcode);
        bytes[1] = (byte)payload.Length;
        Array.Copy(payload, 0, bytes, 2, payload.Length);
        return bytes;
    }

    private static byte[] BuildMaskedTextFrame(string text, byte[] mask)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var masked = new byte[payload.Length];
        for (var index = 0; index < payload.Length; index++)
        {
            masked[index] = (byte)(payload[index] ^ mask[index % 4]);
        }

        var bytes = new byte[2 + 4 + masked.Length];
        bytes[0] = 0x81;
        bytes[1] = (byte)(0x80 | masked.Length);
        Array.Copy(mask, 0, bytes, 2, 4);
        Array.Copy(masked, 0, bytes, 6, masked.Length);
        return bytes;
    }

    private static byte[] BuildUnmaskedControlFrame(WebSocketOpcode opcode)
    {
        return new byte[] { (byte)(0x80 | (byte)opcode), 0x00 };
    }
}
