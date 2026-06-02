using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2PushPromiseParser" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2PushPromiseParserTests
{
    /// <summary>
    ///     An unpadded payload exposes the promised stream id and the fragment.
    /// </summary>
    [Test]
    public async Task Parse_UnpaddedPayload_ExposesPromiseAndFragment()
    {
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x04,
            0x82, 0x86,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.PromisedStreamIdentifier).IsEqualTo((uint)4);
        await Assert.That(result.Value.HeaderBlockFragment.ToArray()).IsEquivalentTo(new byte[] { 0x82, 0x86 });
    }

    /// <summary>
    ///     The top reserved bit of the promised stream id is masked off.
    /// </summary>
    [Test]
    public async Task Parse_ReservedBitSet_IsMasked()
    {
        byte[] payload =
        [
            0x80, 0x00, 0x00, 0x04,
            0x82,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result!.Value.PromisedStreamIdentifier).IsEqualTo((uint)4);
    }

    /// <summary>
    ///     With PADDED set the first octet is the pad length and the trailing padding is excluded from the fragment.
    /// </summary>
    [Test]
    public async Task Parse_PaddedPayload_ExcludesPaddingFromFragment()
    {
        byte[] payload =
        [
            0x02,
            0x00, 0x00, 0x00, 0x04,
            0x82, 0x86,
            0x00, 0x00,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.PromisedStreamIdentifier).IsEqualTo((uint)4);
        await Assert.That(result.Value.HeaderBlockFragment.ToArray()).IsEquivalentTo(new byte[] { 0x82, 0x86 });
    }

    /// <summary>
    ///     A payload shorter than the mandatory 4-octet promised-stream-id field is malformed.
    /// </summary>
    [Test]
    public async Task Parse_TooShortPayload_ReturnsNull()
    {
        byte[] payload = [0x00, 0x00, 0x00];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A pad length that exceeds the available payload is malformed.
    /// </summary>
    [Test]
    public async Task Parse_PaddingExceedsPayload_ReturnsNull()
    {
        byte[] payload =
        [
            0xFF,
            0x00, 0x00, 0x00, 0x04,
            0x82,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A padded payload with PADDED set but a zero-length payload is malformed.
    /// </summary>
    [Test]
    public async Task Parse_PaddedEmptyPayload_ReturnsNull()
    {
        byte[] payload = [];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: true);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     A promised stream id of zero is invalid per RFC 7540 § 6.6 and is rejected.
    /// </summary>
    [Test]
    public async Task Parse_ZeroPromisedStreamId_ReturnsNull()
    {
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x00,
            0x82,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     An odd (client-initiated) promised stream id is invalid for PUSH_PROMISE and is rejected.
    /// </summary>
    [Test]
    public async Task Parse_OddPromisedStreamId_ReturnsNull()
    {
        byte[] payload =
        [
            0x00, 0x00, 0x00, 0x03,
            0x82,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    ///     The reserved-bit mask is applied before stream-id validation: the reserved high bit must be
    ///     masked off so that a non-zero even masked id is accepted regardless of the reserved bit.
    /// </summary>
    [Test]
    public async Task Parse_ReservedBitSetWithEvenStreamId_IsAccepted()
    {
        byte[] payload =
        [
            0x80, 0x00, 0x00, 0x02,
            0x82,
        ];

        var result = HypertextTransferProtocolVersion2PushPromiseParser.Parse(payload, hasPaddedFlag: false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.PromisedStreamIdentifier).IsEqualTo((uint)2);
    }
}
