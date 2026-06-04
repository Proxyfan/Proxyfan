using System;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Thrown by <see cref="ContentEncodingDecoder" /> when the decompressed output exceeds a
///     configured byte-count ceiling or compression-ratio ceiling, guarding against
///     decompression-bomb attacks.
/// </summary>
public sealed class DecompressionLimitExceededException : Exception
{
    /// <summary>
    ///     Gets the size of the compressed input in bytes.
    /// </summary>
    public long CompressedSize { get; }

    /// <summary>
    ///     Gets the number of decompressed bytes read before aborting.
    /// </summary>
    public long DecompressedSoFar { get; }

    /// <summary>
    ///     Gets the configured decompressed-byte ceiling that was breached.
    /// </summary>
    public long MaxDecompressedBytes { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="DecompressionLimitExceededException" />.
    /// </summary>
    /// <param name="compressedSize">The size of the compressed input in bytes.</param>
    /// <param name="decompressedSoFar">The number of decompressed bytes read before aborting.</param>
    /// <param name="maxDecompressedBytes">The configured decompressed-byte ceiling that was breached.</param>
    public DecompressionLimitExceededException(long compressedSize, long decompressedSoFar, long maxDecompressedBytes)
        : base(
            $"Decompressed size limit exceeded: read {decompressedSoFar:N0} bytes "
            + $"(limit {maxDecompressedBytes:N0}) from {compressedSize:N0} compressed bytes.")
    {
        CompressedSize = compressedSize;
        DecompressedSoFar = decompressedSoFar;
        MaxDecompressedBytes = maxDecompressedBytes;
    }
}
