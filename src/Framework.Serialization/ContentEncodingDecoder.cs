using System;
using System.IO;
using System.IO.Compression;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Decodes HTTP response bodies that were compressed using gzip, deflate, or brotli per
///     the Content-Encoding response header.
/// </summary>
public static class ContentEncodingDecoder
{
    /// <summary>
    ///     Decodes the supplied bytes using the algorithm named in the Content-Encoding header
    ///     value. Returns the input bytes unchanged when the encoding is null, empty, or
    ///     "identity"/"none".
    /// </summary>
    /// <param name="contentEncoding">The Content-Encoding header value.</param>
    /// <param name="bytes">The compressed (or uncompressed) bytes.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="NotSupportedException">
    ///     Thrown when the encoding is recognized as a name but not implemented.
    /// </exception>
    public static byte[] Decode(string? contentEncoding, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(contentEncoding))
        {
            return bytes;
        }

        var normalized = contentEncoding.Trim().ToLowerInvariant();

        if (normalized is "identity" or "none")
        {
            return bytes;
        }

        if (string.Equals(normalized, "gzip", StringComparison.Ordinal))
        {
            return DecodeWithStream(bytes, CreateGzipDecompressionStream);
        }

        if (string.Equals(normalized, "deflate", StringComparison.Ordinal))
        {
            return DecodeWithStream(bytes, CreateDeflateDecompressionStream);
        }

        if (string.Equals(normalized, "br", StringComparison.Ordinal))
        {
            return DecodeWithStream(bytes, CreateBrotliDecompressionStream);
        }

        throw new NotSupportedException($"Content encoding '{contentEncoding}' is not supported.");
    }

    private static Stream CreateBrotliDecompressionStream(Stream source)
    {
        var stream = new BrotliStream(source, CompressionMode.Decompress);
        return stream;
    }

    private static Stream CreateDeflateDecompressionStream(Stream source)
    {
        var stream = new DeflateStream(source, CompressionMode.Decompress);
        return stream;
    }

    private static Stream CreateGzipDecompressionStream(Stream source)
    {
        var stream = new GZipStream(source, CompressionMode.Decompress);
        return stream;
    }

    private static byte[] DecodeWithStream(byte[] bytes, DecompressionStreamFactory wrapperFactory)
    {
        using var source = new MemoryStream(bytes);
        using var decompressor = wrapperFactory(source);
        using var destination = new MemoryStream();
        decompressor.CopyTo(destination);
        return destination.ToArray();
    }
}
