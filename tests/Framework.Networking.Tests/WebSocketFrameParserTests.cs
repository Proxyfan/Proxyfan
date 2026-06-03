using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="WebSocketFrameParser" /> covering all RFC 6455 payload-length
///     and masking variations.
/// </summary>
public sealed class WebSocketFrameParserTests
{
    /// <summary>
    ///     Verifies that an empty buffer returns null (more bytes required).
    /// </summary>
    [Test]
    public async Task TryParse_EmptyBuffer_ReturnsNull()
    {
        var frame = WebSocketFrameParser.TryParse(System.Array.Empty<byte>());

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies a single-byte buffer returns null.
    /// </summary>
    [Test]
    public async Task TryParse_OneByteBuffer_ReturnsNull()
    {
        var frame = WebSocketFrameParser.TryParse(new byte[] { 0x81 });

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies that an unmasked text frame with a short payload parses correctly.
    /// </summary>
    [Test]
    public async Task TryParse_UnmaskedShortText_ParsesPayload()
    {
        var payloadText = "hello";
        var payloadBytes = Encoding.UTF8.GetBytes(payloadText);
        var bytes = new byte[2 + payloadBytes.Length];
        bytes[0] = 0x81;
        bytes[1] = (byte)payloadBytes.Length;
        payloadBytes.CopyTo(bytes, 2);

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.IsFinalFragment).IsTrue();
        await Assert.That(frame.Opcode).IsEqualTo(Proxyfan.Domain.Traffic.WebSocketOpcode.Text);
        await Assert.That(Encoding.UTF8.GetString(frame.Payload.Span)).IsEqualTo("hello");
        await Assert.That(frame.TotalLength).IsEqualTo(bytes.Length);
    }

    /// <summary>
    ///     Verifies that a masked client→server text frame is correctly unmasked.
    /// </summary>
    [Test]
    public async Task TryParse_MaskedShortText_UnmasksPayload()
    {
        var payloadBytes = Encoding.UTF8.GetBytes("hi");
        var key = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var masked = new byte[payloadBytes.Length];
        for (var index = 0; index < payloadBytes.Length; index++)
        {
            masked[index] = (byte)(payloadBytes[index] ^ key[index % 4]);
        }

        var bytes = new byte[2 + 4 + payloadBytes.Length];
        bytes[0] = 0x81;
        bytes[1] = (byte)(0x80 | payloadBytes.Length);
        key.CopyTo(bytes, 2);
        masked.CopyTo(bytes, 6);

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(Encoding.UTF8.GetString(frame!.Payload.Span)).IsEqualTo("hi");
    }

    /// <summary>
    ///     Verifies that the 16-bit extended length (length indicator 126) is honored.
    /// </summary>
    [Test]
    public async Task TryParse_ExtendedLength16Bit_ParsesPayload()
    {
        var payload = new byte[200];
        for (var index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)index;
        }

        var bytes = new byte[2 + 2 + payload.Length];
        bytes[0] = 0x82;
        bytes[1] = 126;
        bytes[2] = (byte)(payload.Length >> 8);
        bytes[3] = (byte)(payload.Length & 0xFF);
        payload.CopyTo(bytes, 4);

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Payload.Length).IsEqualTo(payload.Length);
    }

    /// <summary>
    ///     Verifies that the 64-bit extended length (length indicator 127) is honored.
    /// </summary>
    [Test]
    public async Task TryParse_ExtendedLength64Bit_ParsesPayload()
    {
        const int payloadLength = 70000;
        var payload = new byte[payloadLength];
        var bytes = new byte[2 + 8 + payloadLength];
        bytes[0] = 0x82;
        bytes[1] = 127;
        bytes[2] = 0;
        bytes[3] = 0;
        bytes[4] = 0;
        bytes[5] = 0;
        bytes[6] = 0;
        bytes[7] = (byte)((payloadLength >> 16) & 0xFF);
        bytes[8] = (byte)((payloadLength >> 8) & 0xFF);
        bytes[9] = (byte)(payloadLength & 0xFF);
        payload.CopyTo(bytes, 10);

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Payload.Length).IsEqualTo(payloadLength);
    }

    /// <summary>
    ///     Verifies that an unknown opcode throws InvalidDataException.
    /// </summary>
    [Test]
    public async Task TryParse_UnknownOpcode_Throws()
    {
        var bytes = new byte[] { 0x83, 0x00 };

        await Assert.That(() => WebSocketFrameParser.TryParse(bytes)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a control frame with payload &gt; 125 throws.
    /// </summary>
    [Test]
    public async Task TryParse_OversizedControlFrame_Throws()
    {
        var bytes = new byte[2 + 2 + 200];
        bytes[0] = 0x89;
        bytes[1] = 126;
        bytes[2] = 0;
        bytes[3] = 200;

        await Assert.That(() => WebSocketFrameParser.TryParse(bytes)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a buffer truncated mid-payload returns null.
    /// </summary>
    [Test]
    public async Task TryParse_PayloadTruncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x81, 0x05, (byte)'h' };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies that a buffer truncated mid-mask-key returns null.
    /// </summary>
    [Test]
    public async Task TryParse_MaskKeyTruncated_ReturnsNull()
    {
        var bytes = new byte[] { 0x81, 0x85, 0xAA, 0xBB };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies that a buffer truncated mid 16-bit length returns null.
    /// </summary>
    [Test]
    public async Task TryParse_ExtendedLength16TruncatedHeader_ReturnsNull()
    {
        var bytes = new byte[] { 0x82, 126, 0x00 };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies that a buffer truncated mid 64-bit length returns null.
    /// </summary>
    [Test]
    public async Task TryParse_ExtendedLength64TruncatedHeader_ReturnsNull()
    {
        var bytes = new byte[] { 0x82, 127, 0x00, 0x00 };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNull();
    }

    /// <summary>
    ///     Verifies that the FIN bit is read correctly for non-final fragments.
    /// </summary>
    [Test]
    public async Task TryParse_NonFinalFragment_FinBitFalse()
    {
        var bytes = new byte[] { 0x01, 0x00 };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.IsFinalFragment).IsFalse();
    }

    /// <summary>
    ///     Verifies that ping/pong/close opcodes parse correctly.
    /// </summary>
    /// <param name="opcodeByte">First byte (FIN bit set + opcode).</param>
    /// <param name="expected">Expected opcode.</param>
    [Test]
    [Arguments((byte)0x89, Proxyfan.Domain.Traffic.WebSocketOpcode.Ping)]
    [Arguments((byte)0x8A, Proxyfan.Domain.Traffic.WebSocketOpcode.Pong)]
    [Arguments((byte)0x88, Proxyfan.Domain.Traffic.WebSocketOpcode.Close)]
    public async Task TryParse_ControlOpcodes_ParsedCorrectly(byte opcodeByte, Proxyfan.Domain.Traffic.WebSocketOpcode expected)
    {
        var bytes = new byte[] { opcodeByte, 0x00 };

        var frame = WebSocketFrameParser.TryParse(bytes);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Opcode).IsEqualTo(expected);
    }

    /// <summary>
    ///     Verifies that frames with any RSV bit set throw InvalidDataException, per RFC 6455
    ///     §5.2 — reserved bits must be zero unless an extension defines non-zero values.
    /// </summary>
    /// <param name="firstByte">First byte with FIN, an RSV bit, and Text opcode.</param>
    [Test]
    [Arguments((byte)0xC1)] // FIN + RSV1 + Text
    [Arguments((byte)0xA1)] // FIN + RSV2 + Text
    [Arguments((byte)0x91)] // FIN + RSV3 + Text
    [Arguments((byte)0xF1)] // FIN + RSV1 + RSV2 + RSV3 + Text
    public async Task TryParse_ReservedBitsSet_Throws(byte firstByte)
    {
        var bytes = new byte[] { firstByte, 0x00 };

        await Assert.That(() => WebSocketFrameParser.TryParse(bytes)).Throws<InvalidDataException>();
    }
}
