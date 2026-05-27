using System;
using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Low-level helpers that emit the three HPACK header representations: indexed
///     (RFC 7541 § 6.1), literal-with-name-index (§ 6.2.x with non-zero index), and
///     literal-with-literal-name (§ 6.2.x with index zero).
/// </summary>
public static class HypertextTransferProtocolVersion2HpackIndexedWriter
{
    /// <summary>
    ///     Writes an indexed header field (high bit set) referencing the supplied
    ///     combined static-or-dynamic-table <paramref name="index" />.
    /// </summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="index">The 1-based combined HPACK index.</param>
    public static void WriteIndexed(MemoryStream output, int index)
    {
        Span<byte> buffer = stackalloc byte[6];
        var bytes = HypertextTransferProtocolVersion2HpackInteger.Encode(index, 7, 0x80, buffer);
        output.Write(buffer[..bytes]);
    }

    /// <summary>
    ///     Writes a literal representation whose name is provided inline (index 0) and
    ///     whose value follows as a second string literal.
    /// </summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <param name="layout">The flag-byte and prefix-bit layout for this literal kind.</param>
    public static void WriteLiteralWithLiteralName(
        MemoryStream output,
        string name,
        string value,
        HypertextTransferProtocolVersion2HpackLiteralLayout layout)
    {
        Span<byte> indexBuffer = stackalloc byte[6];
        var indexBytes = HypertextTransferProtocolVersion2HpackInteger.Encode(0, layout.PrefixBits, layout.FlagByte, indexBuffer);
        output.Write(indexBuffer[..indexBytes]);
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(output, name);
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(output, value);
    }

    /// <summary>
    ///     Writes a literal representation whose name is taken from <paramref name="nameIndex" />
    ///     and whose value follows as a string literal.
    /// </summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="nameIndex">The combined HPACK index whose name should be reused.</param>
    /// <param name="value">The header value.</param>
    /// <param name="layout">The flag-byte and prefix-bit layout for this literal kind.</param>
    public static void WriteLiteralWithNameIndex(
        MemoryStream output,
        int nameIndex,
        string value,
        HypertextTransferProtocolVersion2HpackLiteralLayout layout)
    {
        Span<byte> indexBuffer = stackalloc byte[6];
        var indexBytes = HypertextTransferProtocolVersion2HpackInteger.Encode(nameIndex, layout.PrefixBits, layout.FlagByte, indexBuffer);
        output.Write(indexBuffer[..indexBytes]);
        HypertextTransferProtocolVersion2HpackStringDecoder.Encode(output, value);
    }
}
