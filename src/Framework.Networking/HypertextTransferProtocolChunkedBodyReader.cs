using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Reads HTTP/1.1 chunked transfer-coding bodies from a <see cref="PipeReader" /> per RFC 7230
///     § 4.1. The reader decodes chunks (size lines, chunk data, optional trailers, terminating
///     <c>0\r\n\r\n</c>) and returns the concatenated decoded body bytes. Chunk extensions are
///     accepted but ignored. Trailer headers are accepted but discarded.
/// </summary>
public static class HypertextTransferProtocolChunkedBodyReader
{
    private const long MaxBodyBytes = 256L * 1024L * 1024L;
    private const long MaxChunkSize = 64L * 1024L * 1024L;

    /// <summary>
    ///     Reads a complete chunked body from <paramref name="reader" /> and returns the decoded
    ///     bytes. Returns <see langword="null" /> when the body is malformed (invalid chunk size,
    ///     premature EOF, oversized chunk, or total body exceeding the safety cap).
    /// </summary>
    /// <param name="reader">The pipe reader positioned at the first chunk-size line.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The decoded body bytes, or <see langword="null" /> on malformed input.</returns>
    public static async Task<byte[]?> ReadAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        using var bodyStream = new MemoryStream();

        while (true)
        {
            var chunkSize = await ReadChunkSizeLineAsync(reader, cancellationToken).ConfigureAwait(false);

            if (chunkSize is null || chunkSize.Value > MaxChunkSize)
            {
                return null;
            }

            if (chunkSize.Value == 0)
            {
                var trailersConsumed = await ConsumeTrailersAsync(reader, cancellationToken).ConfigureAwait(false);
                return trailersConsumed ? bodyStream.ToArray() : null;
            }

            if (bodyStream.Length + chunkSize.Value > MaxBodyBytes)
            {
                return null;
            }

            var chunkCopied = await CopyChunkDataAsync(reader, chunkSize.Value, bodyStream, cancellationToken).ConfigureAwait(false);

            if (!chunkCopied)
            {
                return null;
            }

            var terminatorConsumed = await ConsumeCarriageReturnLineFeedAsync(reader, cancellationToken).ConfigureAwait(false);

            if (!terminatorConsumed)
            {
                return null;
            }
        }
    }

    private static async Task<bool> ConsumeCarriageReturnLineFeedAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var line = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);
        return line is { Length: 0 };
    }

    private static async Task<bool> ConsumeTrailersAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);

            if (line is null)
            {
                return false;
            }

            if (line.Length == 0)
            {
                return true;
            }
        }
    }

    private static async Task<bool> CopyChunkDataAsync(
        PipeReader reader,
        long chunkSize,
        MemoryStream destination,
        CancellationToken cancellationToken)
    {
        var remaining = chunkSize;

        while (remaining > 0)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted)
            {
                reader.AdvanceTo(buffer.End);
                return false;
            }

            var available = Math.Min(remaining, buffer.Length);
            var slice = buffer.Slice(0, available);

            foreach (var memory in slice)
            {
                destination.Write(memory.Span);
            }

            remaining -= available;
            var consumedPosition = buffer.GetPosition(available);
            reader.AdvanceTo(consumedPosition, buffer.End);
        }

        return true;
    }

    private static byte[] CopySequenceToArray(ReadOnlySequence<byte> sequence)
    {
        var buffer = new byte[sequence.Length];
        var destination = buffer.AsSpan();
        var destinationIndex = 0;

        foreach (var memory in sequence)
        {
            memory.Span.CopyTo(destination[destinationIndex..]);
            destinationIndex += memory.Length;
        }

        return buffer;
    }

    private static int FindCarriageReturnLineFeed(byte[] bytes)
    {
        for (var index = 0; index < bytes.Length - 1; index++)
        {
            if (bytes[index] == (byte)'\r' && bytes[index + 1] == (byte)'\n')
            {
                return index;
            }
        }

        return -1;
    }

    private static async Task<long?> ReadChunkSizeLineAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var line = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);

        if (line is null)
        {
            return null;
        }

        var semicolonIndex = line.IndexOf(';');
        var hexPart = semicolonIndex >= 0 ? line[..semicolonIndex] : line;
        var trimmed = hexPart.Trim();

        if (trimmed.Length == 0)
        {
            return null;
        }

        if (!long.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var size) || size < 0)
        {
            return null;
        }

        return size;
    }

    private static async Task<string?> ReadLineAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            var bytes = CopySequenceToArray(buffer);
            var lineTerminatorIndex = FindCarriageReturnLineFeed(bytes);

            if (lineTerminatorIndex >= 0)
            {
                var line = Encoding.ASCII.GetString(bytes, 0, lineTerminatorIndex);
                var consumedPosition = buffer.GetPosition(lineTerminatorIndex + 2);
                reader.AdvanceTo(consumedPosition, buffer.End);
                return line;
            }

            if (result.IsCompleted)
            {
                reader.AdvanceTo(buffer.End);
                return null;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }
}
