using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketMessageAssembler" />.
/// </summary>
public sealed class WebSocketMessageAssemblerTests
{
    /// <summary>
    ///     Verifies that a single FIN-set text frame is emitted as a complete message.
    /// </summary>
    [Test]
    public async Task Accept_SingleFinalTextFrame_EmitsMessage()
    {
        var assembler = new WebSocketMessageAssembler();
        var payload = new byte[] { 1, 2, 3 };
        var frame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Text, payload, totalLength: 5);
        var timestamp = DateTimeOffset.UtcNow;

        var message = assembler.Accept(frame, WebSocketDirection.Inbound, timestamp);

        await Assert.That(message).IsNotNull();
        await Assert.That(message!.Opcode).IsEqualTo(WebSocketOpcode.Text);
        await Assert.That(message.Payload.Length).IsEqualTo(3);
        await Assert.That(assembler.IsAccumulating).IsFalse();
    }

    /// <summary>
    ///     Verifies that a fragmented message (initial frame + continuation + final) is reassembled.
    /// </summary>
    [Test]
    public async Task Accept_FragmentedMessage_ReassemblesPayload()
    {
        var assembler = new WebSocketMessageAssembler();
        var firstFrame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Text, new byte[] { 1, 2 }, 4);
        var continuationFrame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Continuation, new byte[] { 3, 4 }, 4);
        var finalFrame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Continuation, new byte[] { 5 }, 3);
        var timestamp = DateTimeOffset.UtcNow;

        await Assert.That(assembler.Accept(firstFrame, WebSocketDirection.Outbound, timestamp)).IsNull();
        await Assert.That(assembler.Accept(continuationFrame, WebSocketDirection.Outbound, timestamp)).IsNull();
        var message = assembler.Accept(finalFrame, WebSocketDirection.Outbound, timestamp);

        await Assert.That(message).IsNotNull();
        await Assert.That(message!.Payload.Length).IsEqualTo(5);
        await Assert.That(message.Payload.Span[4]).IsEqualTo((byte)5);
    }

    /// <summary>
    ///     Verifies that control frames (Ping/Pong/Close) are emitted immediately and don't
    ///     interfere with an in-progress fragmented message.
    /// </summary>
    [Test]
    public async Task Accept_ControlFrameDuringFragmentation_EmitsControlImmediately()
    {
        var assembler = new WebSocketMessageAssembler();
        var firstFrame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Text, new byte[] { 1, 2 }, 4);
        var pingFrame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Ping, System.Array.Empty<byte>(), 2);
        var finalFrame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Continuation, new byte[] { 3 }, 3);
        var timestamp = DateTimeOffset.UtcNow;

        await Assert.That(assembler.Accept(firstFrame, WebSocketDirection.Outbound, timestamp)).IsNull();
        var pingMessage = assembler.Accept(pingFrame, WebSocketDirection.Outbound, timestamp);
        await Assert.That(pingMessage).IsNotNull();
        await Assert.That(pingMessage!.Opcode).IsEqualTo(WebSocketOpcode.Ping);

        var finalMessage = assembler.Accept(finalFrame, WebSocketDirection.Outbound, timestamp);
        await Assert.That(finalMessage).IsNotNull();
        await Assert.That(finalMessage!.Payload.Length).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that a continuation frame received with no pending message throws.
    /// </summary>
    [Test]
    public async Task Accept_OrphanContinuation_Throws()
    {
        var assembler = new WebSocketMessageAssembler();
        var continuationFrame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Continuation, new byte[] { 1 }, 3);

        await Assert.That(() => assembler.Accept(continuationFrame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow))
            .Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that starting a new text/binary message while one is in progress throws.
    /// </summary>
    [Test]
    public async Task Accept_NewDataMessageWhileInProgress_Throws()
    {
        var assembler = new WebSocketMessageAssembler();
        var firstFrame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Text, new byte[] { 1 }, 3);
        var secondFrame = new WebSocketFrame(isFinalFragment: true, WebSocketOpcode.Binary, new byte[] { 2 }, 3);

        assembler.Accept(firstFrame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow);

        await Assert.That(() => assembler.Accept(secondFrame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow))
            .Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a fragmented (FIN=0) Close control frame is rejected per RFC 6455 Â§ 5.4.
    /// </summary>
    [Test]
    public async Task Accept_FragmentedCloseFrame_Throws()
    {
        var assembler = new WebSocketMessageAssembler();
        var frame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Close, System.Array.Empty<byte>(), 2);

        await Assert.That(() => assembler.Accept(frame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow))
            .Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a fragmented (FIN=0) Ping control frame is rejected per RFC 6455 Â§ 5.4.
    /// </summary>
    [Test]
    public async Task Accept_FragmentedPingFrame_Throws()
    {
        var assembler = new WebSocketMessageAssembler();
        var frame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Ping, System.Array.Empty<byte>(), 2);

        await Assert.That(() => assembler.Accept(frame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow))
            .Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a fragmented (FIN=0) Pong control frame is rejected per RFC 6455 Â§ 5.4.
    /// </summary>
    [Test]
    public async Task Accept_FragmentedPongFrame_Throws()
    {
        var assembler = new WebSocketMessageAssembler();
        var frame = new WebSocketFrame(isFinalFragment: false, WebSocketOpcode.Pong, System.Array.Empty<byte>(), 2);

        await Assert.That(() => assembler.Accept(frame, WebSocketDirection.Inbound, DateTimeOffset.UtcNow))
            .Throws<InvalidDataException>();
    }
}

