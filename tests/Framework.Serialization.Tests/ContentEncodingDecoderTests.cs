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

    private static byte[] Encode(string text, EncodeFactory wrapperFactory)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var destination = new MemoryStream();

        using (var compressor = wrapperFactory(destination))
        {
            compressor.Write(bytes, 0, bytes.Length);
        }

        return destination.ToArray();
    }

    private delegate Stream EncodeFactory(Stream destination);
}
