using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HPACK Huffman codec per RFC 7541 Appendix B. The codec encodes and decodes a
///     fixed 256-symbol code table. Encoded bit streams are padded with the most
///     significant bits of the end-of-string symbol (all 1-bits); padding longer than
///     7 bits or any non-1 padding is a decoding error.
/// </summary>
public static class HypertextTransferProtocolVersion2HpackHuffman
{
    private static readonly uint[] Codes;
    private static readonly byte[] Lengths;
    private static readonly HuffmanDecodeNode Root;

    static HypertextTransferProtocolVersion2HpackHuffman()
    {
        Codes = HypertextTransferProtocolVersion2HpackHuffmanTable.Codes;
        Lengths = HypertextTransferProtocolVersion2HpackHuffmanTable.Lengths;
        Root = BuildDecodeTree(Codes, Lengths);
    }

    /// <summary>
    ///     Attempts to decode the Huffman-encoded <paramref name="source" /> into a byte sequence.
    ///     Returns <c>null</c> when the bit stream contains an EOS symbol mid-stream, the trailing
    ///     padding is more than 7 bits long, or the padding contains a 0-bit.
    /// </summary>
    /// <param name="source">The Huffman-encoded bytes to decode.</param>
    /// <returns>The decoded byte sequence on success; <c>null</c> when the stream is invalid.</returns>
    public static byte[]? Decode(ReadOnlySpan<byte> source)
    {
        var bitLength = source.Length << 3;
        var output = new System.IO.MemoryStream(source.Length);
        var node = Root;
        var bitsConsumedSinceSymbol = 0;
        for (var bitIndex = 0; bitIndex < bitLength; bitIndex++)
        {
            var bit = (source[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1;
            node = bit == 0 ? node.Zero! : node.One!;
            bitsConsumedSinceSymbol++;
            if (node.IsTerminal)
            {
                if (node.Symbol == 256)
                {
                    return null;
                }
                output.WriteByte((byte)node.Symbol);
                node = Root;
                bitsConsumedSinceSymbol = 0;
            }
        }
        if (bitsConsumedSinceSymbol > 7)
        {
            return null;
        }
        if (bitsConsumedSinceSymbol > 0)
        {
            var lastByte = source[^1];
            var mask = (1 << bitsConsumedSinceSymbol) - 1;
            if ((lastByte & mask) != mask)
            {
                return null;
            }
        }
        return output.ToArray();
    }

    /// <summary>
    ///     Encodes <paramref name="source" /> into a freshly allocated byte array using HPACK Huffman coding.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Huffman-encoded byte sequence with trailing pad bits set to 1.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> source)
    {
        var bitLength = 0;
        for (var index = 0; index < source.Length; index++)
        {
            bitLength += Lengths[source[index]];
        }
        var byteLength = (bitLength + 7) >> 3;
        var destination = new byte[byteLength];
        var bitOffset = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var symbol = source[index];
            var code = Codes[symbol];
            var length = Lengths[symbol];
            WriteBits(destination, bitOffset, code, length);
            bitOffset += length;
        }
        var padBits = (byteLength << 3) - bitOffset;
        if (padBits > 0)
        {
            destination[byteLength - 1] |= (byte)((1 << padBits) - 1);
        }
        return destination;
    }

    /// <summary>
    ///     Encodes <paramref name="source" /> as a UTF-8 byte sequence then Huffman-codes it.
    /// </summary>
    /// <param name="source">The string to encode.</param>
    /// <returns>The Huffman-encoded byte sequence.</returns>
    public static byte[] EncodeString(string source)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(source);
        return Encode(bytes);
    }

    private static HuffmanDecodeNode BuildDecodeTree(uint[] codes, byte[] lengths)
    {
        var root = new HuffmanDecodeNode();
        for (var symbol = 0; symbol < codes.Length; symbol++)
        {
            var code = codes[symbol];
            var length = lengths[symbol];
            var current = root;
            for (var bit = length - 1; bit >= 0; bit--)
            {
                var goRight = ((code >> bit) & 1) == 1;
                if (goRight)
                {
                    if (current.One is null)
                    {
                        var node = new HuffmanDecodeNode();
                        current.One = node;
                    }
                    current = current.One;
                }
                else
                {
                    if (current.Zero is null)
                    {
                        var node = new HuffmanDecodeNode();
                        current.Zero = node;
                    }
                    current = current.Zero;
                }
            }
            current.IsTerminal = true;
            current.Symbol = symbol;
        }
        return root;
    }

    private static void WriteBits(byte[] destination, int bitOffset, uint code, int bitLength)
    {
        for (var bit = bitLength - 1; bit >= 0; bit--)
        {
            var value = (code >> bit) & 1;
            if (value == 0)
            {
                bitOffset++;
                continue;
            }
            var byteIndex = bitOffset >> 3;
            var bitInByte = 7 - (bitOffset & 7);
            destination[byteIndex] |= (byte)(1 << bitInByte);
            bitOffset++;
        }
    }

    private sealed class HuffmanDecodeNode
    {
        public bool IsTerminal { get; set; }

        public HuffmanDecodeNode? One { get; set; }

        public int Symbol { get; set; }

        public HuffmanDecodeNode? Zero { get; set; }
    }
}
