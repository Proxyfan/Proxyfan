using System;
using System.IO;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Encodes and decodes HPACK string literals per RFC 7541 § 5.2. Strings are prefixed by a
///     7-bit length whose high bit flags whether the payload is Huffman-encoded.
/// </summary>
public static class HypertextTransferProtocolVersion2HpackStringDecoder
{
    /// <summary>
    ///     Decodes a single string literal beginning at <paramref name="source" />[0].
    /// </summary>
    /// <param name="source">The source bytes positioned at the length prefix.</param>
    /// <returns>The decoded string plus the number of bytes consumed.</returns>
    /// <exception cref="FormatException">When the literal is truncated or Huffman-malformed.</exception>
    public static HypertextTransferProtocolVersion2HpackStringDecodeResult Decode(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            throw new FormatException("Truncated HPACK string literal.");
        }
        var isHuffman = (source[0] & 0x80) == 0x80;
        var lengthResult = HypertextTransferProtocolVersion2HpackInteger.Decode(source, 7)
            ?? throw new FormatException("Malformed HPACK string length integer.");
        var offset = lengthResult.BytesConsumed;
        var length = lengthResult.Value;
        if (offset + length > source.Length)
        {
            throw new FormatException("HPACK string literal extends past header block.");
        }
        var payload = source.Slice(offset, length);
        offset += length;
        if (isHuffman)
        {
            var decoded = HypertextTransferProtocolVersion2HpackHuffman.Decode(payload)
                ?? throw new FormatException("Malformed HPACK Huffman string.");
            var value = Encoding.UTF8.GetString(decoded);
            return new HypertextTransferProtocolVersion2HpackStringDecodeResult(value, offset);
        }
        var raw = Encoding.UTF8.GetString(payload);
        return new HypertextTransferProtocolVersion2HpackStringDecodeResult(raw, offset);
    }

    /// <summary>
    ///     Appends a Huffman-or-raw string literal for <paramref name="value" /> to
    ///     <paramref name="output" />.
    /// </summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="value">The string to emit.</param>
    public static void Encode(MemoryStream output, string value)
    {
        var rawBytes = Encoding.UTF8.GetBytes(value);
        var huffmanBytes = HypertextTransferProtocolVersion2HpackHuffman.Encode(rawBytes);
        Span<byte> lengthBuffer = stackalloc byte[6];
        if (huffmanBytes.Length < rawBytes.Length)
        {
            var lengthBytes = HypertextTransferProtocolVersion2HpackInteger.Encode(huffmanBytes.Length, 7, 0x80, lengthBuffer);
            output.Write(lengthBuffer[..lengthBytes]);
            output.Write(huffmanBytes);
        }
        else
        {
            var lengthBytes = HypertextTransferProtocolVersion2HpackInteger.Encode(rawBytes.Length, 7, 0x00, lengthBuffer);
            output.Write(lengthBuffer[..lengthBytes]);
            output.Write(rawBytes);
        }
    }
}
