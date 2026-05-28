using System;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2HpackHuffman" /> using the worked
///     vectors from RFC 7541 Appendix C.4.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackHuffmanTests
{
    /// <summary>
    ///     RFC 7541 C.4.1: the Huffman encoding of "www.example.com" is
    ///     <c>f1 e3 c2 e5 f2 3a 6b a0 ab 90 f4 ff</c>.
    /// </summary>
    [Test]
    public async Task Encode_WwwExampleCom_ProducesRfc7541Vector()
    {
        var encoded = HypertextTransferProtocolVersion2HpackHuffman.EncodeString("www.example.com");
        byte[] expected = [0xF1, 0xE3, 0xC2, 0xE5, 0xF2, 0x3A, 0x6B, 0xA0, 0xAB, 0x90, 0xF4, 0xFF];

        await Assert.That(encoded).IsEquivalentTo(expected);
    }

    /// <summary>
    ///     RFC 7541 C.4.4: "no-cache" Huffman-encodes to <c>a8 eb 10 64 9c bf</c>.
    /// </summary>
    [Test]
    public async Task Encode_NoCache_ProducesRfc7541Vector()
    {
        var encoded = HypertextTransferProtocolVersion2HpackHuffman.EncodeString("no-cache");
        byte[] expected = [0xA8, 0xEB, 0x10, 0x64, 0x9C, 0xBF];

        await Assert.That(encoded).IsEquivalentTo(expected);
    }

    /// <summary>
    ///     Round-trips a representative selection of strings through Huffman encode and decode.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("www.example.com")]
    [Arguments("no-cache")]
    [Arguments("custom-key")]
    [Arguments("custom-value")]
    [Arguments("Hello, World! 1234567890 !@#$%^&*()")]
    public async Task EncodeThenDecode_VariousInputs_RoundTripsExactBytes(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);

        var encoded = HypertextTransferProtocolVersion2HpackHuffman.Encode(inputBytes);
        var decoded = HypertextTransferProtocolVersion2HpackHuffman.Decode(encoded);

        await Assert.That(decoded).IsNotNull();
        await Assert.That(decoded!).IsEquivalentTo(inputBytes);
    }

    /// <summary>
    ///     A padding byte that contains a 0-bit is malformed and decoder returns <c>null</c>.
    /// </summary>
    [Test]
    public async Task Decode_PaddingContainsZero_ReturnsNull()
    {
        var encoded = HypertextTransferProtocolVersion2HpackHuffman.EncodeString("a");
        encoded[^1] &= 0xFE;

        var decoded = HypertextTransferProtocolVersion2HpackHuffman.Decode(encoded);

        await Assert.That(decoded).IsNull();
    }

    /// <summary>
    ///     A trailing run of pad bits longer than 7 bits is malformed and the decoder returns
    ///     <c>null</c>.
    /// </summary>
    [Test]
    public async Task Decode_OversizedPadding_ReturnsNull()
    {
        byte[] tooMuchPadding = [0xFF];

        var decoded = HypertextTransferProtocolVersion2HpackHuffman.Decode(tooMuchPadding);

        await Assert.That(decoded).IsNull();
    }

    /// <summary>
    ///     An empty input encodes to an empty byte sequence.
    /// </summary>
    [Test]
    public async Task Encode_Empty_ReturnsEmptyArray()
    {
        var encoded = HypertextTransferProtocolVersion2HpackHuffman.Encode(ReadOnlySpan<byte>.Empty);

        await Assert.That(encoded.Length).IsEqualTo(0);
    }

}
