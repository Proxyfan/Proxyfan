using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackStringDecoder" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackStringDecoderTests
{
    [Test]
    public async Task Decode_RawString_ReturnsValueWithExactByteCount()
    {
        var raw = "abc"u8;
        using var memory = new MemoryStream();
        memory.WriteByte((byte)raw.Length);
        memory.Write(raw);
        var encoded = memory.ToArray();

        var result = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded);

        await Assert.That(result.Value).IsEqualTo("abc");
        await Assert.That(result.BytesConsumed).IsEqualTo(4);
    }

    [Test]
    public async Task Decode_HuffmanEncodedString_ReturnsDecodedValue()
    {
        using var encoded = new MemoryStream();
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(encoded, "www.example.com");

        var bytes = encoded.ToArray();

        var result = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(bytes);

        await Assert.That(result.Value).IsEqualTo("www.example.com");
    }

    [Test]
    public async Task Decode_EmptyBuffer_ThrowsFormatException()
    {
        var thrown = false;
        try
        {
            HypertextTransferProtocolVersion2HpackStringDecoder.Decode(ReadOnlySpan<byte>.Empty);
        }
        catch (FormatException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
    }

    [Test]
    public async Task Decode_LengthExtendsPastBuffer_ThrowsFormatException()
    {
        var encoded = new byte[] { 0x05, (byte)'a', (byte)'b' };
        var thrown = false;
        try
        {
            HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded);
        }
        catch (FormatException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
    }

    [Test]
    public async Task Encode_LongRawString_RoundTripsWithDecode()
    {
        const string value = "the quick brown fox jumps over the lazy dog";
        using var encoded = new MemoryStream();
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(encoded, value);

        var result = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded.ToArray());

        await Assert.That(result.Value).IsEqualTo(value);
    }

    [Test]
    public async Task Encode_LongAsciiString_PrefersHuffman()
    {
        using var encoded = new MemoryStream();
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(encoded, "the quick brown fox jumps over the lazy dog");

        var bytes = encoded.ToArray();

        await Assert.That((bytes[0] & 0x80) == 0x80).IsTrue();
    }

    [Test]
    public async Task Encode_EmptyString_PrefersRawEncoding()
    {
        using var encoded = new MemoryStream();
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(encoded, string.Empty);

        var bytes = encoded.ToArray();

        await Assert.That(bytes.Length).IsEqualTo(1);
        await Assert.That(bytes[0]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Encode_NonAsciiPayload_RoundTripsWithDecode()
    {
        // HPACK string literals carry opaque header octets (RFC 9110 § 5.5 field-vchar +
        // obs-text), so any byte 0x00..0xFF must survive a round-trip unchanged.
        const string value = "café \u00ff\u0080\u00fe";
        using var encoded = new MemoryStream();
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(encoded, value);

        var result = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded.ToArray());

        await Assert.That(result.Value).IsEqualTo(value);
    }

    [Test]
    public async Task Decode_RawNonUtf8Octets_PreservesEveryByte()
    {
        // A raw literal whose payload is not valid UTF-8 must not be lossily replaced with
        // U+FFFD; each byte must map one-to-one to the corresponding Latin-1 code point.
        var payload = new byte[] { 0xC0, 0xC1, 0xF5, 0xFF, 0x80 };
        using var memory = new MemoryStream();
        memory.WriteByte((byte)payload.Length);
        memory.Write(payload);

        var result = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(memory.ToArray());

        await Assert.That(result.Value).IsEqualTo("\u00c0\u00c1\u00f5\u00ff\u0080");
    }

    /// <summary>
    ///     A single 0x7F prefix byte (length 127, continuation flag set) without any continuation
    ///     bytes is a truncated HPACK integer; the decoder must throw because the length
    ///     <see cref="HypertextTransferProtocolVersion2HpackInteger.Decode" /> returns null.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedLengthInteger_ThrowsFormatException()
    {
        var encoded = new byte[] { 0x7F };
        var thrown = false;
        try
        {
            HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded);
        }
        catch (FormatException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
    }

    /// <summary>
    ///     A Huffman-flagged literal whose payload is not a valid Huffman sequence (a single
    ///     trailing 0xFF byte) must produce a <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Decode_HuffmanFlaggedButPayloadMalformed_ThrowsFormatException()
    {
        var encoded = new byte[] { 0x81, 0xFF };
        var thrown = false;
        try
        {
            HypertextTransferProtocolVersion2HpackStringDecoder.Decode(encoded);
        }
        catch (FormatException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
    }
}
