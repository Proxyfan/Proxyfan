using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HPACK N-bit prefix integer codec as defined by RFC 7541 § 5.1. Integers
///     under <c>(2^N) - 1</c> fit in the prefix; larger values overflow into
///     continuation bytes with a 7-bit payload and an 8th-bit continuation flag.
/// </summary>
public static class HypertextTransferProtocolVersion2HpackInteger
{
    /// <summary>
    ///     Attempts to decode an integer from <paramref name="source" /> starting at the first byte.
    ///     Returns <c>null</c> when the buffer is insufficient or when the encoded value overflows
    ///     <see cref="int.MaxValue" />.
    /// </summary>
    /// <param name="source">The source span containing encoded bytes.</param>
    /// <param name="prefixBits">The number of bits in the first-byte prefix (1-8).</param>
    /// <returns>
    ///     The decoded value and bytes consumed on success; <c>null</c> on buffer underflow
    ///     or overflow.
    /// </returns>
    public static HypertextTransferProtocolVersion2HpackIntegerDecodeResult? Decode(ReadOnlySpan<byte> source, int prefixBits)
    {
        if (source.Length == 0)
        {
            return null;
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(prefixBits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixBits, 8);
        var prefixMax = (1 << prefixBits) - 1;
        var first = source[0] & prefixMax;
        if (first < prefixMax)
        {
            return new HypertextTransferProtocolVersion2HpackIntegerDecodeResult(first, 1);
        }
        var accumulator = (long)prefixMax;
        var shift = 0;
        var index = 1;
        while (true)
        {
            if (index >= source.Length)
            {
                return null;
            }
            var current = source[index];
            index++;
            accumulator += (long)(current & 0x7F) << shift;
            if (accumulator > int.MaxValue)
            {
                return null;
            }
            if ((current & 0x80) == 0)
            {
                return new HypertextTransferProtocolVersion2HpackIntegerDecodeResult((int)accumulator, index);
            }
            shift += 7;
            if (shift >= 32)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Encodes <paramref name="value" /> into <paramref name="destination" /> using a
    ///     <paramref name="prefixBits" />-bit prefix. The first byte's low <paramref name="prefixBits" />
    ///     bits are replaced; high bits in <paramref name="firstByteFlags" /> are preserved.
    /// </summary>
    /// <param name="value">The non-negative integer value to encode.</param>
    /// <param name="prefixBits">The number of bits in the first-byte prefix (1-8).</param>
    /// <param name="firstByteFlags">The high-order bits to OR into the first byte (e.g. representation flag).</param>
    /// <param name="destination">The destination span. Must be large enough to hold the encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public static int Encode(int value, int prefixBits, byte firstByteFlags, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(prefixBits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixBits, 8);
        var prefixMax = (1 << prefixBits) - 1;
        if (value < prefixMax)
        {
            destination[0] = (byte)(firstByteFlags | value);
            return 1;
        }
        destination[0] = (byte)(firstByteFlags | prefixMax);
        var remainder = value - prefixMax;
        var written = 1;
        while (remainder >= 0x80)
        {
            destination[written] = (byte)((remainder & 0x7F) | 0x80);
            written++;
            remainder >>= 7;
        }
        destination[written] = (byte)remainder;
        written++;
        return written;
    }
}
