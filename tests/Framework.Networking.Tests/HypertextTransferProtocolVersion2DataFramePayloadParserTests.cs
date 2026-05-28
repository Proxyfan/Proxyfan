using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2DataFramePayloadParser" />,
///     covering padded and unpadded DATA payloads plus malformed-payload edge cases.
/// </summary>
public sealed class HypertextTransferProtocolVersion2DataFramePayloadParserTests
{
    /// <summary>
    ///     When PADDED is unset the payload is the application data verbatim.
    /// </summary>
    [Test]
    public async Task Parse_NoPadding_ReturnsEntirePayload()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });

        var result = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Length).IsEqualTo(4);
        await Assert.That(result!.Value.Span.SequenceEqual(payload.Span)).IsTrue();
    }

    /// <summary>
    ///     When PADDED is set, the first octet is Pad Length and the trailing Pad-Length octets
    ///     are stripped.
    /// </summary>
    [Test]
    public async Task Parse_PaddedPayload_StripsPadLengthAndTrailingPadding()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 3, 0x10, 0x20, 0x30, 0x40, 0x50, 0xFF, 0xFF, 0xFF });

        var result = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Length).IsEqualTo(5);
        await Assert.That(result!.Value.Span.SequenceEqual(new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 })).IsTrue();
    }

    /// <summary>
    ///     A padded payload whose declared Pad Length is zero contains no padding tail.
    /// </summary>
    [Test]
    public async Task Parse_ZeroPadLength_ReturnsDataWithoutTail()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 0, 0xAA, 0xBB });

        var result = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result!.Value.Span.SequenceEqual(new byte[] { 0xAA, 0xBB })).IsTrue();
    }

    /// <summary>
    ///     PADDED set but payload contains no Pad Length octet — malformed.
    /// </summary>
    [Test]
    public async Task Parse_PaddedFlagWithEmptyPayload_ReturnsNull()
    {
        var result = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(ReadOnlyMemory<byte>.Empty, hasPaddedFlag: true);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     Pad Length larger than the remaining payload — FRAME_SIZE_ERROR.
    /// </summary>
    [Test]
    public async Task Parse_PadLengthExceedsRemainingPayload_ReturnsNull()
    {
        var payload = new ReadOnlyMemory<byte>(new byte[] { 5, 0xAA, 0xBB });

        var result = HypertextTransferProtocolVersion2DataFramePayloadParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result.HasValue).IsFalse();
    }
}
