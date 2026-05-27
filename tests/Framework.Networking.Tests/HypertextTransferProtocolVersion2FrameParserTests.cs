using System;
using System.IO;
using System.Threading.Tasks;
using Proxyfan.Framework.Networking;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Framework.Networking.Tests;

public sealed class HypertextTransferProtocolVersion2FrameParserTests
{
    [Test]
    public async Task TryParseHeader_BufferShorterThanNineBytes_ReturnsNull()
    {
        var buffer = new byte[8];

        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(header).IsNull();
    }

    [Test]
    public async Task TryParseHeader_PingFrame_ParsesAllFields()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x08,
            0x06,
            0x01,
            0x00, 0x00, 0x00, 0x00,
        };

        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.Length).IsEqualTo(8);
        await Assert.That(header.RawType).IsEqualTo((byte)0x06);
        await Assert.That(header.IsKnownType).IsTrue();
        await Assert.That(header.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.Ping);
        await Assert.That(header.Flags).IsEqualTo(HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge);
        await Assert.That(header.StreamIdentifier).IsEqualTo(0u);
    }

    [Test]
    public async Task TryParseHeader_HeadersFrameOnNonZeroStream_ParsesStreamIdentifier()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x04,
            0x01,
            0x04,
            0x00, 0x00, 0x00, 0x05,
        };

        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.StreamIdentifier).IsEqualTo(5u);
        await Assert.That(header.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.Headers);
        await Assert.That(header.Flags).IsEqualTo(HypertextTransferProtocolVersion2FrameFlag.EndHeaders);
    }

    [Test]
    public async Task TryParseHeader_ReservedHighBitSetOnStreamId_IsIgnoredAndMaskedOut()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x00,
            0x06,
            0x00,
            0x80, 0x00, 0x00, 0x07,
        };

        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.StreamIdentifier).IsEqualTo(7u);
    }

    [Test]
    public async Task TryParseHeader_UnknownFrameType_ReturnsHeaderWithIsKnownTypeFalse()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x00,
            0xFE,
            0x00,
            0x00, 0x00, 0x00, 0x00,
        };

        var header = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(header).IsNotNull();
        await Assert.That(header!.IsKnownType).IsFalse();
        await Assert.That(header.RawType).IsEqualTo((byte)0xFE);
    }

    [Test]
    public async Task TryParse_PayloadIncomplete_ReturnsNull()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x08,
            0x06,
            0x00,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x02,
        };

        var frame = HypertextTransferProtocolVersion2FrameParser.TryParse(buffer);

        await Assert.That(frame).IsNull();
    }

    [Test]
    public async Task TryParse_PingFrameWithEightBytePayload_ReturnsCompleteFrame()
    {
        var buffer = new byte[]
        {
            0x00, 0x00, 0x08,
            0x06,
            0x00,
            0x00, 0x00, 0x00, 0x00,
            0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04,
        };

        var frame = HypertextTransferProtocolVersion2FrameParser.TryParse(buffer);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Header.Length).IsEqualTo(8);
        await Assert.That(frame.Payload.Length).IsEqualTo(8);
        await Assert.That(frame.Payload.ToArray()).IsEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 });
    }

    [Test]
    public async Task TryParse_BufferShorterThanHeader_ReturnsNull()
    {
        var buffer = new byte[5];

        var frame = HypertextTransferProtocolVersion2FrameParser.TryParse(buffer);

        await Assert.That(frame).IsNull();
    }

    [Test]
    public async Task TryParse_MaximumDeclaredLength_ParsesLengthCorrectly()
    {
        var buffer = new byte[9 + 1];
        buffer[0] = 0x00;
        buffer[1] = 0x00;
        buffer[2] = 0x01;
        buffer[3] = (byte)HypertextTransferProtocolVersion2FrameType.Data;
        buffer[4] = 0x00;
        buffer[5] = 0x00;
        buffer[6] = 0x00;
        buffer[7] = 0x00;
        buffer[8] = 0x09;
        buffer[9] = 0x42;

        var frame = HypertextTransferProtocolVersion2FrameParser.TryParse(buffer);

        await Assert.That(frame).IsNotNull();
        await Assert.That(frame!.Header.Length).IsEqualTo(1);
        await Assert.That(frame.Payload.Span[0]).IsEqualTo((byte)0x42);
        await Assert.That(frame.Header.StreamIdentifier).IsEqualTo(9u);
    }
}
