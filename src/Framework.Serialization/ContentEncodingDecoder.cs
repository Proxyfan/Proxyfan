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
    ///     Default maximum number of decompressed bytes allowed per call (100 MiB).
    ///     Callers may override this via the overload that accepts explicit limits.
    /// </summary>
    public const long DefaultMaxDecompressedBytes = 100L * 1024 * 1024;

    /// <summary>
    ///     Default maximum allowed ratio of decompressed-to-compressed bytes (200×).
    ///     Callers may override this via the overload that accepts explicit limits.
    /// </summary>
    public const double DefaultMaxDecompressionRatio = 200.0;
    private const int CopyBufferSize = 81920;

    /// <summary>
    ///     Decodes the supplied bytes using the algorithm(s) named in the Content-Encoding header
    ///     value, using the default decompression-size and ratio limits.
    ///     The header may be a single token (for example <c>gzip</c>) or an RFC 7231
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
    /// <exception cref="DecompressionLimitExceededException">
    ///     Thrown when the decompressed output exceeds <see cref="DefaultMaxDecompressedBytes" /> or
    ///     <see cref="DefaultMaxDecompressionRatio" />.
    /// </exception>
    public static byte[] Decode(string? contentEncoding, byte[] bytes)
    {
        return Decode(contentEncoding, bytes, DefaultMaxDecompressedBytes, DefaultMaxDecompressionRatio);
    }

    /// <summary>
    ///     Decodes the supplied bytes using the algorithm(s) named in the Content-Encoding header
    ///     value. The header may be a single token (for example <c>gzip</c>) or an RFC 7231
    ///     comma-separated chain (for example <c>gzip, br</c>); chained encodings are unwrapped in
    ///     reverse order. Returns the input bytes unchanged when the encoding is null, empty, or
    ///     consists solely of "identity"/"none" tokens.
    /// </summary>
    /// <param name="contentEncoding">The Content-Encoding header value.</param>
    /// <param name="bytes">The compressed (or uncompressed) bytes.</param>
    /// <param name="maxDecompressedBytes">
    ///     Hard ceiling on total decompressed output bytes.
    ///     Pass <see cref="long.MaxValue" /> to disable the byte-count limit.
    /// </param>
    /// <param name="maxDecompressionRatio">
    ///     Hard ceiling on the ratio of decompressed-to-compressed bytes.
    ///     Pass <see cref="double.MaxValue" /> or a non-positive value to disable the ratio limit.
    /// </param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="NotSupportedException">
    ///     Thrown when any token in the chain is recognized as a name but not implemented.
    /// </exception>
    /// <exception cref="DecompressionLimitExceededException">
    ///     Thrown when the decompressed output exceeds <paramref name="maxDecompressedBytes" /> or
    ///     <paramref name="maxDecompressionRatio" />.
    /// </exception>
    public static byte[] Decode(string? contentEncoding, byte[] bytes, long maxDecompressedBytes, double maxDecompressionRatio)
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
                current = DecodeWithStream(current, CreateGzipDecompressionStream, maxDecompressedBytes, maxDecompressionRatio);
            }
            else if (string.Equals(token, "deflate", StringComparison.Ordinal))
            {
                current = DecodeWithStream(current, CreateDeflateDecompressionStream, maxDecompressedBytes, maxDecompressionRatio);
            }
            else if (string.Equals(token, "br", StringComparison.Ordinal))
            {
                current = DecodeWithStream(current, CreateBrotliDecompressionStream, maxDecompressedBytes, maxDecompressionRatio);
            }
            else
            {
                throw new NotSupportedException(
                    $"Content encoding token '{token}' in '{contentEncoding}' is not supported.");
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

    private static byte[] DecodeWithStream(
        byte[] bytes,
        DecompressionStreamFactory wrapperFactory,
        long maxDecompressedBytes,
        double maxDecompressionRatio)
    {
        using var source = new MemoryStream(bytes);
        using var decompressor = wrapperFactory(source);
        using var destination = new MemoryStream();

        var buffer = new byte[CopyBufferSize];
        long totalRead = 0;

        while (true)
        {
            var read = decompressor.Read(buffer, 0, buffer.Length);

            if (read == 0)
            {
                break;
            }

            totalRead += read;

            if (totalRead > maxDecompressedBytes)
            {
                throw new DecompressionLimitExceededException(bytes.LongLength, totalRead, maxDecompressedBytes);
            }

            if (maxDecompressionRatio > 0 && bytes.LongLength > 0
                && (double)totalRead / bytes.LongLength > maxDecompressionRatio)
            {
                throw new DecompressionLimitExceededException(bytes.LongLength, totalRead, maxDecompressedBytes);
            }

            destination.Write(buffer, 0, read);
        }

        return destination.ToArray();
    }
}
