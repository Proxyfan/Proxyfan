using Proxyfan.Domain.Traffic;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2RemoteProcedureCallCapture" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2RemoteProcedureCallCaptureTests
{
    /// <summary>
    ///     A single fully-formed length-prefixed message in one Append produces one captured message.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_SingleCompleteFrame_ProducesOneMessage()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var frame = BuildFrame(isCompressed: false, new byte[] { 0xAA, 0xBB, 0xCC });

        capture.AppendClientBytes(frame);

        await Assert.That(flow.Messages.Count).IsEqualTo(1);
        await Assert.That(flow.Messages[0].Direction).IsEqualTo(RemoteProcedureCallDirection.Outbound);
        await Assert.That(flow.Messages[0].IsCompressed).IsFalse();
        await Assert.That(flow.Messages[0].Payload.Length).IsEqualTo(3);
    }

    /// <summary>
    ///     A length-prefixed message split across two Append calls is still captured once.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_FrameSplitAcrossAppends_ProducesOneMessage()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var frame = BuildFrame(isCompressed: false, new byte[] { 0x11, 0x22, 0x33, 0x44 });

        capture.AppendClientBytes(frame.AsSpan(0, 3));
        capture.AppendClientBytes(frame.AsSpan(3));

        await Assert.That(flow.Messages.Count).IsEqualTo(1);
        await Assert.That(flow.Messages[0].Payload.Length).IsEqualTo(4);
    }

    /// <summary>
    ///     The compression flag is propagated to the captured message.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_CompressedFrame_PreservesCompressionFlag()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var frame = BuildFrame(isCompressed: true, new byte[] { 0x01 });

        capture.AppendClientBytes(frame);

        await Assert.That(flow.Messages.Count).IsEqualTo(1);
        await Assert.That(flow.Messages[0].IsCompressed).IsTrue();
    }

    /// <summary>
    ///     Upstream bytes produce response-direction messages.
    /// </summary>
    [Test]
    public async Task AppendUpstreamBytes_CompleteFrame_ProducesInboundMessage()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var frame = BuildFrame(isCompressed: false, new byte[] { 0xDE, 0xAD });

        capture.AppendUpstreamBytes(frame);

        await Assert.That(flow.Messages.Count).IsEqualTo(1);
        await Assert.That(flow.Messages[0].Direction).IsEqualTo(RemoteProcedureCallDirection.Inbound);
    }

    /// <summary>
    ///     Two back-to-back messages in one Append both surface.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_TwoFramesBackToBack_ProducesTwoMessages()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var first = BuildFrame(isCompressed: false, new byte[] { 0x01, 0x02 });
        var second = BuildFrame(isCompressed: false, new byte[] { 0x03 });
        var combined = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, combined, 0, first.Length);
        Buffer.BlockCopy(second, 0, combined, first.Length, second.Length);

        capture.AppendClientBytes(combined);

        await Assert.That(flow.Messages.Count).IsEqualTo(2);
        await Assert.That(flow.Messages[0].Payload.Length).IsEqualTo(2);
        await Assert.That(flow.Messages[1].Payload.Length).IsEqualTo(1);
    }

    /// <summary>
    ///     A truncated message header is buffered until enough bytes arrive.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_PartialHeader_HoldsUntilCompleted()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var frame = BuildFrame(isCompressed: false, new byte[] { 0x10, 0x20, 0x30 });

        capture.AppendClientBytes(frame.AsSpan(0, 2));

        await Assert.That(flow.Messages.Count).IsEqualTo(0);

        capture.AppendClientBytes(frame.AsSpan(2));

        await Assert.That(flow.Messages.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     The Flow property surfaces the wrapped flow exactly.
    /// </summary>
    [Test]
    public async Task Flow_ReturnsWrappedFlow_IsSameReference()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);

        await Assert.That(capture.Flow).IsSameReferenceAs(flow);
    }

    /// <summary>
    ///     A frame whose declared length overflows int.MaxValue is swallowed silently so the
    ///     buffer cannot run away with malformed length-prefixes.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_LengthExceedsInt32Max_NoMessageProducedAndNoThrow()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var oversize = new byte[5];
        oversize[0] = 0x00;
        oversize[1] = 0xFF;
        oversize[2] = 0xFF;
        oversize[3] = 0xFF;
        oversize[4] = 0xFF;

        capture.AppendClientBytes(oversize);

        await Assert.That(flow.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     The same oversize behaviour applies in the upstream direction.
    /// </summary>
    [Test]
    public async Task AppendUpstreamBytes_LengthExceedsInt32Max_NoMessageProducedAndNoThrow()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var oversize = new byte[5];
        oversize[0] = 0x00;
        oversize[1] = 0xFF;
        oversize[2] = 0xFF;
        oversize[3] = 0xFF;
        oversize[4] = 0xFF;

        capture.AppendUpstreamBytes(oversize);

        await Assert.That(flow.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When one Append contains one complete frame plus a partial header of the next, the
    ///     leftover bytes are buffered and resolved when the next Append supplies the rest.
    /// </summary>
    [Test]
    public async Task AppendClientBytes_CompleteFrameThenPartialNextFrame_PreservesLeftoverBytes()
    {
        var flow = CreateFlow();
        var capture = CreateCapture(flow);
        var first = BuildFrame(isCompressed: false, new byte[] { 0x99 });
        var second = BuildFrame(isCompressed: false, new byte[] { 0x77, 0x88 });
        var combined = new byte[first.Length + 2];
        Buffer.BlockCopy(first, 0, combined, 0, first.Length);
        Buffer.BlockCopy(second, 0, combined, first.Length, 2);

        capture.AppendClientBytes(combined);
        await Assert.That(flow.Messages.Count).IsEqualTo(1);

        capture.AppendClientBytes(second.AsSpan(2));

        await Assert.That(flow.Messages.Count).IsEqualTo(2);
        await Assert.That(flow.Messages[1].Payload.Length).IsEqualTo(2);
    }

    private static byte[] BuildFrame(bool isCompressed, byte[] payload)
    {
        var frame = new byte[5 + payload.Length];
        frame[0] = isCompressed ? (byte)1 : (byte)0;
        var length = (uint)payload.Length;
        frame[1] = (byte)((length >> 24) & 0xFF);
        frame[2] = (byte)((length >> 16) & 0xFF);
        frame[3] = (byte)((length >> 8) & 0xFF);
        frame[4] = (byte)(length & 0xFF);
        Buffer.BlockCopy(payload, 0, frame, 5, payload.Length);
        return frame;
    }

    private static HypertextTransferProtocolVersion2RemoteProcedureCallCapture CreateCapture(RemoteProcedureCallFlow flow)
    {
        var capture = new HypertextTransferProtocolVersion2RemoteProcedureCallCapture(flow, TimeProvider.System);
        return capture;
    }

    private static RemoteProcedureCallFlow CreateFlow()
    {
        var traffic = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:0", DateTimeOffset.UtcNow);
        var flow = new RemoteProcedureCallFlow(traffic);
        return flow;
    }
}
