using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HeadersFramePayloadParser" />,
///     covering plain, padded, priority-bearing, and padded+priority HEADERS payloads.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HeadersFramePayloadParserTests
{
    /// <summary>
    ///     When neither PADDED nor PRIORITY is set the payload is the header block fragment verbatim.
    /// </summary>
    [Test]
    public async Task Parse_NoFlags_ReturnsEntirePayload()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 0x82, 0x86, 0x84 });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: false, hasPriorityFlag: false);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Span.SequenceEqual(payload.Span)).IsTrue();
    }

    /// <summary>
    ///     When PADDED is set, the first octet is Pad Length and the trailing Pad-Length
    ///     octets are stripped from the fragment.
    /// </summary>
    [Test]
    public async Task Parse_PaddedNoPriority_StripsPadLengthAndTail()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 2, 0x82, 0x86, 0x84, 0xFF, 0xFF });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: true, hasPriorityFlag: false);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Span.SequenceEqual(new byte[] { 0x82, 0x86, 0x84 })).IsTrue();
    }

    /// <summary>
    ///     When PRIORITY is set, the first 5 octets (4-byte stream dependency + 1-byte weight)
    ///     are stripped from the front of the fragment.
    /// </summary>
    [Test]
    public async Task Parse_PriorityNoPadding_StripsFiveOctetPriorityBlock()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 0x80, 0x00, 0x00, 0x01, 0x10, 0x82, 0x86, 0x84 });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: false, hasPriorityFlag: true);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Span.SequenceEqual(new byte[] { 0x82, 0x86, 0x84 })).IsTrue();
    }

    /// <summary>
    ///     When both PADDED and PRIORITY are set the parser strips Pad Length, the 5-octet
    ///     priority block, and the trailing padding bytes.
    /// </summary>
    [Test]
    public async Task Parse_PaddedAndPriority_StripsBothPadAndPriority()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[]
        {
            1,
            0x00, 0x00, 0x00, 0x05, 0x07,
            0x82, 0x86, 0x84,
            0xFF,
        });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: true, hasPriorityFlag: true);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Span.SequenceEqual(new byte[] { 0x82, 0x86, 0x84 })).IsTrue();
    }

    /// <summary>
    ///     PADDED set but payload contains no Pad Length octet — malformed.
    /// </summary>
    [Test]
    public async Task Parse_PaddedFlagWithEmptyPayload_ReturnsNull()
    {
        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(ReadOnlyMemory<byte>.Empty, hasPaddedFlag: true, hasPriorityFlag: false);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     PRIORITY set but payload too short to contain the 5-octet dependency block —
    ///     malformed.
    /// </summary>
    [Test]
    public async Task Parse_PriorityFlagWithUnderSizedPayload_ReturnsNull()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 0x00, 0x00, 0x01, 0x05 });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: false, hasPriorityFlag: true);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     Pad Length larger than remaining payload — FRAME_SIZE_ERROR.
    /// </summary>
    [Test]
    public async Task Parse_PadLengthExceedsPayload_ReturnsNull()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 10, 0x82 });

        var result = HypertextTransferProtocolVersion2HeadersFramePayloadParser.Parse(payload, hasPaddedFlag: true, hasPriorityFlag: false);

        await Assert.That(result.HasValue).IsFalse();
    }
}
