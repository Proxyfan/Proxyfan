using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2FrameReader" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2FrameReaderTests
{
    /// <summary>
    ///     A complete frame written to the pipe is read in one call.
    /// </summary>
    [Test]
    public async Task ReadFrameAsync_CompleteFrameAvailable_ReturnsFrame()
    {
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        var buffer = new byte[9 + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            StreamIdentifier = 13,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(buffer);
        await pipe.Writer.CompleteAsync();

        var frame = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Header.StreamIdentifier).IsEqualTo((uint)13);
        await Assert.That(frame.Header.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.Data);
        await Assert.That(frame.Payload.ToArray()).IsEquivalentTo(payload);
    }

    /// <summary>
    ///     Two complete frames are read back-to-back from the pipe.
    /// </summary>
    [Test]
    public async Task ReadFrameAsync_TwoFrames_BothAreReturned()
    {
        var first = BuildHeaderFrame(1);
        var second = BuildHeaderFrame(3);
        var combined = first.Concat(second).ToArray();
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(combined);
        await pipe.Writer.CompleteAsync();

        var firstResult = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);
        var secondResult = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(firstResult!.Header.StreamIdentifier).IsEqualTo((uint)1);
        await Assert.That(secondResult!.Header.StreamIdentifier).IsEqualTo((uint)3);
    }

    /// <summary>
    ///     A pipe that completes with no bytes returns null.
    /// </summary>
    [Test]
    public async Task ReadFrameAsync_CleanlyClosedPipe_ReturnsNull()
    {
        var pipe = new Pipe();
        await pipe.Writer.CompleteAsync();

        var frame = await HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     A frame split across two writes is still read correctly.
    /// </summary>
    [Test]
    public async Task ReadFrameAsync_FrameSplitAcrossWrites_ReturnsFrame()
    {
        var full = BuildHeaderFrame(5);
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(full.AsMemory(0, 4));
        await pipe.Writer.FlushAsync();
        var readTask = HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);
        await pipe.Writer.WriteAsync(full.AsMemory(4));
        await pipe.Writer.CompleteAsync();

        var frame = await readTask;

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Header.StreamIdentifier).IsEqualTo((uint)5);
    }

    /// <summary>
    ///     When the header bytes arrive before the payload bytes, the reader waits for the rest
    ///     of the payload to arrive instead of returning a truncated frame. Exercises the
    ///     buffer-shorter-than-payload branch in <c>TryConsumeFrame</c>.
    /// </summary>
    [Test]
    public async Task ReadFrameAsync_PayloadArrivesAfterHeader_ReturnsFrame()
    {
        var payload = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
        var buffer = new byte[9 + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 7,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(buffer.AsMemory(0, 9));
        await pipe.Writer.FlushAsync();
        var readTask = HypertextTransferProtocolVersion2FrameReader.ReadFrameAsync(pipe.Reader, CancellationToken.None);
        await pipe.Writer.WriteAsync(buffer.AsMemory(9));
        await pipe.Writer.CompleteAsync();

        var frame = await readTask;

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Header.StreamIdentifier).IsEqualTo((uint)7);
        await Assert.That(frame.Payload.ToArray()).IsEquivalentTo(payload);
    }

    private static byte[] BuildHeaderFrame(uint streamId)
    {
        var payload = new byte[] { 0x82 };
        var buffer = new byte[9 + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length,
            Type = HypertextTransferProtocolVersion2FrameType.Headers,
            Flags = HypertextTransferProtocolVersion2FrameFlag.EndHeaders | HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            StreamIdentifier = streamId,
        };
        HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        return buffer;
    }
}
