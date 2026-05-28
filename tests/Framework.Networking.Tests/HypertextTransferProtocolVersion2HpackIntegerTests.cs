using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackInteger" /> using the RFC 7541
///     § 5.1 worked examples and edge cases around buffer underflow and overflow.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackIntegerTests
{
    /// <summary>
    ///     RFC 7541 § 5.1 example 1: 10 in a 5-bit prefix encodes to a single byte 0x0A.
    /// </summary>
    [Test]
    public async Task Decode_TenIn5BitPrefix_ReturnsTen()
    {
        byte[] input = [0x0A];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 5);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Value).IsEqualTo(10);
        await Assert.That(result.Value.BytesConsumed).IsEqualTo(1);
    }

    /// <summary>
    ///     RFC 7541 § 5.1 example 2: 1337 in a 5-bit prefix encodes to 0x1F 0x9A 0x0A.
    /// </summary>
    [Test]
    public async Task Decode_ThirteenThirtySevenIn5BitPrefix_ReturnsThirteenThirtySeven()
    {
        byte[] input = [0x1F, 0x9A, 0x0A];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 5);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Value).IsEqualTo(1337);
        await Assert.That(result.Value.BytesConsumed).IsEqualTo(3);
    }

    /// <summary>
    ///     RFC 7541 § 5.1 example 3: 42 in an 8-bit prefix encodes to a single byte 0x2A.
    /// </summary>
    [Test]
    public async Task Decode_FortyTwoIn8BitPrefix_ReturnsFortyTwo()
    {
        byte[] input = [0x2A];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 8);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Value).IsEqualTo(42);
        await Assert.That(result.Value.BytesConsumed).IsEqualTo(1);
    }

    /// <summary>
    ///     An empty buffer cannot encode any value and the decoder must report failure.
    /// </summary>
    [Test]
    public async Task Decode_EmptyBuffer_ReturnsNull()
    {
        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(ReadOnlySpan<byte>.Empty, 5);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     A truncated continuation sequence must yield <c>null</c>.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedContinuation_ReturnsNull()
    {
        byte[] input = [0x1F, 0x9A];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 5);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     Values that overflow <see cref="int.MaxValue" /> must yield <c>null</c>.
    /// </summary>
    [Test]
    public async Task Decode_Overflow_ReturnsNull()
    {
        byte[] input = [0x1F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 5);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     A sequence of zero-payload continuation bytes that pushes the bit shift past 32
    ///     must yield <c>null</c>. Each 0x80 byte contributes nothing to the accumulator but
    ///     bumps <c>shift</c> by 7.
    /// </summary>
    [Test]
    public async Task Decode_ContinuationShiftOverflow_ReturnsNull()
    {
        byte[] input = [0x1F, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80];

        var result = HypertextTransferProtocolVersion2HpackInteger.Decode(input, 5);

        await Assert.That(result.HasValue).IsFalse();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2HpackInteger.Encode" /> round-trips the
    ///     boundary value where the prefix is fully consumed and continuation bytes start.
    /// </summary>
    [Test]
    [Arguments(0, 5, 1)]
    [Arguments(10, 5, 1)]
    [Arguments(30, 5, 1)]
    [Arguments(31, 5, 2)]
    [Arguments(1337, 5, 3)]
    [Arguments(255, 8, 2)]
    public async Task Encode_KnownValue_RoundTripsViaDecode(int value, int prefixBits, int expectedBytes)
    {
        var buffer = new byte[16];

        var written = HypertextTransferProtocolVersion2HpackInteger.Encode(value, prefixBits, firstByteFlags: 0, buffer);
        var decoded = HypertextTransferProtocolVersion2HpackInteger.Decode(buffer.AsSpan(0, written), prefixBits);

        await Assert.That(written).IsEqualTo(expectedBytes);
        await Assert.That(decoded).IsNotNull();
        await Assert.That(decoded!.Value.Value).IsEqualTo(value);
        await Assert.That(decoded.Value.BytesConsumed).IsEqualTo(written);
    }

    /// <summary>
    ///     The encoder preserves caller-supplied flag bits in the high-order portion of the first byte.
    /// </summary>
    [Test]
    public async Task Encode_WithFlagByte_PreservesHighBits()
    {
        var buffer = new byte[4];

        var written = HypertextTransferProtocolVersion2HpackInteger.Encode(10, prefixBits: 5, firstByteFlags: 0x40, buffer);

        await Assert.That(written).IsEqualTo(1);
        await Assert.That(buffer[0]).IsEqualTo((byte)0x4A);
    }
}
