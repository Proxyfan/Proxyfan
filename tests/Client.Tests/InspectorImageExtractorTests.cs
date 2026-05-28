using Proxyfan.Client.Inspector;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="InspectorImageExtractor" />.
/// </summary>
public sealed class InspectorImageExtractorTests
{
    private static readonly byte[] SamplePngHeader = new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
    };

    [Test]
    public async Task TryExtract_EmptyBody_ReturnsNull()
    {
        var result = InspectorImageExtractor.TryExtract(ReadOnlyMemory<byte>.Empty, HeaderCollection.Empty);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryExtract_NoContentTypeHeader_ReturnsNull()
    {
        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, HeaderCollection.Empty);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryExtract_NonImageContentType_ReturnsNull()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "application/json");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TryExtract_ImagePngWithoutEncoding_ReturnsRawBody()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "image/png");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(SamplePngHeader.Length);
    }

    [Test]
    public async Task TryExtract_ImageJpegMixedCase_ReturnsBody()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "Image/JPEG");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task TryExtract_ImageWithCharsetParameter_ReturnsBody()
    {
        var headers = HeaderCollection.Empty.Add("Content-Type", "image/svg+xml; charset=utf-8");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task TryExtract_ImageWithGzipEncoding_ReturnsDecompressedBytes()
    {
        var compressed = Compress(SamplePngHeader);
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "image/png")
            .Add("Content-Encoding", "gzip");

        var result = InspectorImageExtractor.TryExtract(compressed, headers);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(SamplePngHeader.Length);
    }

    [Test]
    public async Task TryExtract_ImageWithUnknownEncoding_ReturnsRawBody()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "image/png")
            .Add("Content-Encoding", "compress-9000");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(SamplePngHeader.Length);
    }

    [Test]
    public async Task TryExtract_ImageWithCorruptGzip_ReturnsRawBody()
    {
        var headers = HeaderCollection.Empty
            .Add("Content-Type", "image/png")
            .Add("Content-Encoding", "gzip");

        var result = InspectorImageExtractor.TryExtract(SamplePngHeader, headers);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Length).IsEqualTo(SamplePngHeader.Length);
    }

    private static byte[] Compress(byte[] input)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(input, 0, input.Length);
        }

        return output.ToArray();
    }
}
