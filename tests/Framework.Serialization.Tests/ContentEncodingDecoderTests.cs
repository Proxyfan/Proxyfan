using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ContentEncodingDecoder" />.
/// </summary>
public sealed class ContentEncodingDecoderTests
{
    /// <summary>
    ///     Verifies that null/empty encoding returns the input verbatim.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Decode_NullOrEmptyEncoding_ReturnsInputUnchanged(string? encoding)
    {
        var input = new byte[] { 1, 2, 3 };

        var output = ContentEncodingDecoder.Decode(encoding, input);

        await Assert.That(output.Length).IsEqualTo(3);
        await Assert.That(output).IsSameReferenceAs(input);
    }

    /// <summary>
    ///     Verifies that "identity" returns the input verbatim.
    /// </summary>
    [Test]
    public async Task Decode_IdentityEncoding_ReturnsInputUnchanged()
    {
        var input = new byte[] { 1, 2, 3 };

        var output = ContentEncodingDecoder.Decode("identity", input);

        await Assert.That(output).IsSameReferenceAs(input);
    }

    /// <summary>
    ///     Verifies that gzip-encoded bytes round-trip correctly.
    /// </summary>
    [Test]
    public async Task Decode_GzipEncoded_DecodesToOriginal()
    {
        const string original = "hello, world";
        var bytes = Encode(original, source => new GZipStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("gzip", bytes);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that deflate-encoded bytes round-trip correctly.
    /// </summary>
    [Test]
    public async Task Decode_DeflateEncoded_DecodesToOriginal()
    {
        const string original = "deflate works";
        var bytes = Encode(original, source => new DeflateStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("deflate", bytes);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that brotli-encoded bytes round-trip correctly.
    /// </summary>
    [Test]
    public async Task Decode_BrotliEncoded_DecodesToOriginal()
    {
        const string original = "brotli is nice";
        var bytes = Encode(original, source => new BrotliStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("br", bytes);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that encoding-name matching is case-insensitive.
    /// </summary>
    [Test]
    public async Task Decode_UppercaseEncodingName_StillWorks()
    {
        const string original = "case";
        var bytes = Encode(original, source => new GZipStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("GZIP", bytes);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that an unknown encoding throws NotSupportedException.
    /// </summary>
    [Test]
    public async Task Decode_UnknownEncoding_Throws()
    {
        await Assert.That(() => ContentEncodingDecoder.Decode("unknown", new byte[] { 1, 2, 3 }))
            .Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that a comma-separated chain of encodings is unwrapped in reverse order.
    /// </summary>
    [Test]
    public async Task Decode_ChainedGzipThenBrotli_DecodesToOriginal()
    {
        const string original = "stacked encodings";
        var gzipped = Encode(original, source => new GZipStream(source, CompressionMode.Compress));
        var brotliOverGzip = Encode(gzipped, source => new BrotliStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("gzip, br", brotliOverGzip);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that identity tokens within a chain are ignored.
    /// </summary>
    [Test]
    public async Task Decode_ChainWithIdentityToken_DecodesRemainingEncoding()
    {
        const string original = "identity is a no-op";
        var bytes = Encode(original, source => new GZipStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("gzip, identity", bytes);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    /// <summary>
    ///     Verifies that a chain consisting solely of identity tokens returns the input verbatim.
    /// </summary>
    [Test]
    public async Task Decode_ChainOfOnlyIdentityTokens_ReturnsInputUnchanged()
    {
        var input = new byte[] { 9, 8, 7 };

        var output = ContentEncodingDecoder.Decode("identity, none", input);

        await Assert.That(output).IsSameReferenceAs(input);
    }

    /// <summary>
    ///     Verifies that an unknown token within a chain causes the whole chain to fail.
    /// </summary>
    [Test]
    public async Task Decode_ChainWithUnknownToken_Throws()
    {
        var bytes = Encode("anything", source => new GZipStream(source, CompressionMode.Compress));

        await Assert.That(() => ContentEncodingDecoder.Decode("gzip, mystery", bytes))
            .Throws<NotSupportedException>();
    }

    /// <summary>
    ///     Verifies that case-insensitivity holds for each token in a chain.
    /// </summary>
    [Test]
    public async Task Decode_ChainWithMixedCaseTokens_DecodesToOriginal()
    {
        const string original = "mixed case chain";
        var gzipped = Encode(original, source => new GZipStream(source, CompressionMode.Compress));
        var brotliOverGzip = Encode(gzipped, source => new BrotliStream(source, CompressionMode.Compress));

        var decoded = ContentEncodingDecoder.Decode("GZip, BR", brotliOverGzip);

        await Assert.That(Encoding.UTF8.GetString(decoded)).IsEqualTo(original);
    }

    private static byte[] Encode(string text, EncodeFactory wrapperFactory)
    {
        return Encode(Encoding.UTF8.GetBytes(text), wrapperFactory);
    }

    private static byte[] Encode(byte[] bytes, EncodeFactory wrapperFactory)
    {
        using var destination = new MemoryStream();

        using (var compressor = wrapperFactory(destination))
        {
            compressor.Write(bytes, 0, bytes.Length);
        }

        return destination.ToArray();
    }

    private delegate Stream EncodeFactory(Stream destination);
}
