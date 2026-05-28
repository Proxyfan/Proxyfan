using Proxyfan.Domain.Traffic;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallRelay" /> verifying byte-for-byte forwarding
///     and gRPC frame extraction.
/// </summary>
public sealed class RemoteProcedureCallRelayTests
{
    /// <summary>
    ///     A single complete frame is forwarded verbatim and surfaced via the callback.
    /// </summary>
    [Test]
    public async Task RelayAsync_SingleFrame_ForwardsAndCaptures()
    {
        var captured = new List<RemoteProcedureCallCapturedMessage>();
        var relay = new RemoteProcedureCallRelay(
            RemoteProcedureCallDirection.Outbound,
            captured.Add,
            TimeProvider.System);
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        var frame = BuildFrame(false, payload);
        using var source = new MemoryStream(frame);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(captured[0].Direction).IsEqualTo(RemoteProcedureCallDirection.Outbound);
        await Assert.That(captured[0].IsCompressed).IsFalse();
        await Assert.That(captured[0].Payload.ToArray()).IsEquivalentTo(payload);
        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
    }

    /// <summary>
    ///     Two back-to-back frames in a single buffer are both captured.
    /// </summary>
    [Test]
    public async Task RelayAsync_TwoFrames_CapturesBoth()
    {
        var captured = new List<RemoteProcedureCallCapturedMessage>();
        var relay = new RemoteProcedureCallRelay(
            RemoteProcedureCallDirection.Inbound,
            captured.Add,
            TimeProvider.System);
        var first = BuildFrame(false, new byte[] { 0x01 });
        var second = BuildFrame(true, new byte[] { 0x02, 0x03 });
        var combined = new byte[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        using var source = new MemoryStream(combined);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(captured[0].IsCompressed).IsFalse();
        await Assert.That(captured[1].IsCompressed).IsTrue();
        await Assert.That(captured[0].Payload.ToArray()).IsEquivalentTo(new byte[] { 0x01 });
        await Assert.That(captured[1].Payload.ToArray()).IsEquivalentTo(new byte[] { 0x02, 0x03 });
    }

    /// <summary>
    ///     A frame split across two reads is captured exactly once.
    /// </summary>
    [Test]
    public async Task RelayAsync_PartialReadsAcrossFrameBoundary_CapturesOnce()
    {
        var captured = new List<RemoteProcedureCallCapturedMessage>();
        var relay = new RemoteProcedureCallRelay(
            RemoteProcedureCallDirection.Outbound,
            captured.Add,
            TimeProvider.System);
        var payload = new byte[] { 0x10, 0x20, 0x30, 0x40 };
        var frame = BuildFrame(false, payload);
        var splitAt = 3;
        var firstChunk = new byte[splitAt];
        var secondChunk = new byte[frame.Length - splitAt];
        Array.Copy(frame, 0, firstChunk, 0, splitAt);
        Array.Copy(frame, splitAt, secondChunk, 0, frame.Length - splitAt);
        using var source = new ChunkedStream(firstChunk, secondChunk);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured[0].Payload.ToArray()).IsEquivalentTo(payload);
        await Assert.That(destination.ToArray()).IsEquivalentTo(frame);
    }

    /// <summary>
    ///     An empty source returns zero messages.
    /// </summary>
    [Test]
    public async Task RelayAsync_EmptySource_ReturnsZero()
    {
        var captured = new List<RemoteProcedureCallCapturedMessage>();
        var relay = new RemoteProcedureCallRelay(
            RemoteProcedureCallDirection.Outbound,
            captured.Add,
            TimeProvider.System);
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(captured.Count).IsEqualTo(0);
        await Assert.That(destination.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     A single buffer containing one complete frame followed by the partial header of the
    ///     next frame must capture exactly one message and retain the partial bytes for the
    ///     next read. Exercises the <c>if (remaining &gt; 0)</c> branch of
    ///     <c>DrainCompletedMessages</c>.
    /// </summary>
    [Test]
    public async Task RelayAsync_CompleteFramePlusPartialNextHeader_KeepsRemainder()
    {
        var captured = new List<RemoteProcedureCallCapturedMessage>();
        var relay = new RemoteProcedureCallRelay(
            RemoteProcedureCallDirection.Outbound,
            captured.Add,
            TimeProvider.System);
        var firstFrame = BuildFrame(false, new byte[] { 0xAA });
        var secondFrame = BuildFrame(false, new byte[] { 0xBB, 0xCC });
        var partialOfSecond = secondFrame.AsSpan(0, 2).ToArray();
        var combined = new byte[firstFrame.Length + partialOfSecond.Length];
        Array.Copy(firstFrame, 0, combined, 0, firstFrame.Length);
        Array.Copy(partialOfSecond, 0, combined, firstFrame.Length, partialOfSecond.Length);
        var remainingOfSecond = secondFrame.AsSpan(2).ToArray();
        using var source = new ChunkedStream(combined, remainingOfSecond);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(captured[0].Payload.ToArray()).IsEquivalentTo(new byte[] { 0xAA });
        await Assert.That(captured[1].Payload.ToArray()).IsEquivalentTo(new byte[] { 0xBB, 0xCC });
        var expectedDestination = new byte[firstFrame.Length + secondFrame.Length];
        Array.Copy(firstFrame, 0, expectedDestination, 0, firstFrame.Length);
        Array.Copy(secondFrame, 0, expectedDestination, firstFrame.Length, secondFrame.Length);
        await Assert.That(destination.ToArray()).IsEquivalentTo(expectedDestination);
    }

    private static byte[] BuildFrame(bool compressed, byte[] payload)
    {
        var frame = new byte[5 + payload.Length];
        frame[0] = compressed ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1, 4), (uint)payload.Length);
        Array.Copy(payload, 0, frame, 5, payload.Length);
        return frame;
    }
}
