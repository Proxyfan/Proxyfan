using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2FrameWriter" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2FrameWriterTests
{
    /// <summary>
    ///     A header round-trips through the parser yielding equivalent fields.
    /// </summary>
    [Test]
    public async Task WriteHeader_ValidFields_RoundTripsThroughParser()
    {
        var buffer = new byte[9];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = 5,
            Type = HypertextTransferProtocolVersion2FrameType.Headers,
            Flags = HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge | HypertextTransferProtocolVersion2FrameFlag.EndHeaders,
            StreamIdentifier = 7,
        };

        var written = HypertextTransferProtocolVersion2FrameWriter.WriteHeader(buffer, descriptor);
        var parsed = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(written).IsEqualTo(9);
        await Assert.That(parsed).IsNotNull();
        await Assert.That(parsed!.Length).IsEqualTo(5);
        await Assert.That(parsed.Type).IsEqualTo(HypertextTransferProtocolVersion2FrameType.Headers);
        await Assert.That(parsed.Flags).IsEqualTo(HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge | HypertextTransferProtocolVersion2FrameFlag.EndHeaders);
        await Assert.That(parsed.StreamIdentifier).IsEqualTo((uint)7);
    }

    /// <summary>
    ///     The reserved top bit of the stream identifier is masked off on the wire.
    /// </summary>
    [Test]
    public async Task WriteHeader_StreamIdentifierTopBit_IsMasked()
    {
        var buffer = new byte[9];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = 0,
            Type = HypertextTransferProtocolVersion2FrameType.Settings,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 0x80000005,
        };

        HypertextTransferProtocolVersion2FrameWriter.WriteHeader(buffer, descriptor);
        var parsed = HypertextTransferProtocolVersion2FrameParser.TryParseHeader(buffer);

        await Assert.That(parsed!.StreamIdentifier).IsEqualTo((uint)5);
    }

    /// <summary>
    ///     A negative payload length is rejected.
    /// </summary>
    [Test]
    public async Task WriteHeader_NegativeLength_Throws()
    {
        var buffer = new byte[9];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = -1,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 1,
        };

        await Assert.That(() => HypertextTransferProtocolVersion2FrameWriter.WriteHeader(buffer, descriptor))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     A payload length above the 24-bit on-the-wire limit is rejected.
    /// </summary>
    [Test]
    public async Task WriteHeader_PayloadLengthExceedsLimit_Throws()
    {
        var buffer = new byte[9];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = 0x1000000,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 1,
        };

        await Assert.That(() => HypertextTransferProtocolVersion2FrameWriter.WriteHeader(buffer, descriptor))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     A destination buffer shorter than the header length is rejected.
    /// </summary>
    [Test]
    public async Task WriteHeader_ShortDestination_Throws()
    {
        var buffer = new byte[8];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = 0,
            Type = HypertextTransferProtocolVersion2FrameType.Settings,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 0,
        };

        await Assert.That(() => HypertextTransferProtocolVersion2FrameWriter.WriteHeader(buffer, descriptor))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     A complete frame round-trips through the parser preserving the payload bytes.
    /// </summary>
    [Test]
    public async Task WriteFrame_WithPayload_RoundTripsThroughParser()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var buffer = new byte[9 + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.EndStreamOrAcknowledge,
            StreamIdentifier = 11,
        };

        var written = HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload);
        var parsed = HypertextTransferProtocolVersion2FrameParser.TryParse(buffer);

        await Assert.That(written).IsEqualTo(13);
        await Assert.That(parsed!.Header.Length).IsEqualTo(payload.Length);
        await Assert.That(parsed.Header.StreamIdentifier).IsEqualTo((uint)11);
        await Assert.That(parsed.Payload.ToArray()).IsEquivalentTo(payload);
    }

    /// <summary>
    ///     A destination buffer too small to hold header + payload is rejected.
    /// </summary>
    [Test]
    public async Task WriteFrame_DestinationTooSmall_Throws()
    {
        var payload = new byte[16];
        var buffer = new byte[9];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 1,
        };

        await Assert.That(() => HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload))
            .Throws<ArgumentException>();
    }

    /// <summary>
    ///     A descriptor whose payload length disagrees with the supplied payload is rejected.
    /// </summary>
    [Test]
    public async Task WriteFrame_DescriptorPayloadLengthMismatch_Throws()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var buffer = new byte[9 + payload.Length];
        var descriptor = new HypertextTransferProtocolVersion2FrameDescriptor
        {
            PayloadLength = payload.Length + 1,
            Type = HypertextTransferProtocolVersion2FrameType.Data,
            Flags = HypertextTransferProtocolVersion2FrameFlag.None,
            StreamIdentifier = 1,
        };

        await Assert.That(() => HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, payload))
            .Throws<ArgumentException>();
    }
}
