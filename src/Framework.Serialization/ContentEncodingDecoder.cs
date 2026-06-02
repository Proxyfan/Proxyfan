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
    ///     Decodes the supplied bytes using the algorithm(s) named in the Content-Encoding header
    ///     value. The header may be a single token (for example <c>gzip</c>) or an RFC 7231
    ///     comma-separated chain (for example <c>gzip, br</c>); chained encodings are unwrapped in
    ///     reverse order. Returns the input bytes unchanged when the encoding is null, empty, or
    ///     consists solely of "identity"/"none" tokens.
    /// </summary>
    /// <param name="contentEncoding">The Content-Encoding header value.</param>
    /// <param name="bytes">The compressed (or uncompressed) bytes.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="NotSupportedException">
    ///     Thrown when any token in the chain is recognized as a name but not implemented.
    /// </exception>
    public static byte[] Decode(string? contentEncoding, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(contentEncoding))
        {
            return bytes;
        }

        var tokens = contentEncoding.Split(',');
        var current = bytes;
        var anyApplied = false;

        for (var index = tokens.Length - 1; index >= 0; index--)
        {
            var token = tokens[index].Trim().ToLowerInvariant();

            if (token.Length == 0 || token is "identity" or "none")
            {
                continue;
            }

            if (string.Equals(token, "gzip", StringComparison.Ordinal))
            {
                current = DecodeWithStream(current, CreateGzipDecompressionStream);
            }
            else if (string.Equals(token, "deflate", StringComparison.Ordinal))
            {
                current = DecodeWithStream(current, CreateDeflateDecompressionStream);
            }
            else if (string.Equals(token, "br", StringComparison.Ordinal))
            {
                current = DecodeWithStream(current, CreateBrotliDecompressionStream);
            }
            else
            {
                throw new NotSupportedException($"Content encoding '{contentEncoding}' is not supported.");
            }

            anyApplied = true;
        }

        if (!anyApplied)
        {
            return bytes;
        }

        return current;
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
