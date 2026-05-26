using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers for working with <see cref="PipeReader" /> instances.
/// </summary>
public static class PipeReaderHelper
{
    private static readonly byte[] EndOfHeadersSequence;

    static PipeReaderHelper()
    {
        EndOfHeadersSequence = "\r\n\r\n"u8.ToArray();
    }

    /// <summary>
    ///     Reads from <paramref name="reader" /> until at least <paramref name="minimumBytes" />
    ///     bytes are buffered, the pipe completes, or cancellation is requested.
    /// </summary>
    /// <param name="reader">The pipe reader to read from.</param>
    /// <param name="minimumBytes">The minimum number of bytes to buffer before returning.</param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>
    ///     The <see cref="ReadResult" /> containing at least <paramref name="minimumBytes" />
    ///     bytes, or all available bytes if the pipe completed or was cancelled first.
    /// </returns>
    public static async Task<ReadResult> ReadUntilAsync(PipeReader reader, int minimumBytes, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (result.Buffer.Length >= minimumBytes || result.IsCompleted || result.IsCanceled)
            {
                return result;
            }

            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }

    /// <summary>
    ///     Reads from <paramref name="reader" /> until the HTTP end-of-headers sequence
    ///     (<c>\r\n\r\n</c>) is found, consuming only the header bytes while preserving any
    ///     buffered body bytes for subsequent reads. Returns <see langword="null" /> if the
    ///     connection closes, the limit is exceeded, or cancellation is requested before the
    ///     sequence is found.
    /// </summary>
    /// <param name="reader">The pipe reader to read from.</param>
    /// <param name="maxBytes">
    ///     The maximum number of header bytes to accumulate before giving up.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>
    ///     A byte array containing only the header bytes including the final <c>\r\n\r\n</c>,
    ///     or <see langword="null" /> if the headers could not be read.
    /// </returns>
    public static async Task<byte[]?> ReadUntilEndOfHeadersAsync(PipeReader reader, int maxBytes, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            var bufferedBytes = buffer.ToArray();
            var endOfHeadersIndex = GetEndOfHeadersIndex(bufferedBytes);

            if (endOfHeadersIndex >= 0)
            {
                var headerLength = endOfHeadersIndex + EndOfHeadersSequence.Length;

                if (headerLength > maxBytes)
                {
                    reader.AdvanceTo(buffer.End);
                    return null;
                }

                var consumedPosition = buffer.GetPosition(headerLength);
                var headerBytes = new byte[headerLength];
                Array.Copy(bufferedBytes, headerBytes, headerLength);
                reader.AdvanceTo(consumedPosition, buffer.End);
                return headerBytes;
            }

            if (buffer.Length > maxBytes || result.IsCompleted || result.IsCanceled)
            {
                reader.AdvanceTo(buffer.End);
                return null;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static int GetEndOfHeadersIndex(byte[] bytes)
    {
        if (bytes.Length < EndOfHeadersSequence.Length)
        {
            return -1;
        }

        var searchLimit = bytes.Length - EndOfHeadersSequence.Length;

        for (var index = 0; index <= searchLimit; index++)
        {
            if (bytes[index] == EndOfHeadersSequence[0]
                && bytes[index + 1] == EndOfHeadersSequence[1]
                && bytes[index + 2] == EndOfHeadersSequence[2]
                && bytes[index + 3] == EndOfHeadersSequence[3])
            {
                return index;
            }
        }

        return -1;
    }
}