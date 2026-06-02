using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Platform;

/// <summary>
///     Reads an <see cref="HttpContent" /> body into a UTF-8 string while enforcing an
///     upper byte budget. Returns <see langword="null" /> if the body exceeds the budget,
///     so callers can treat oversized responses the same way as malformed ones.
/// </summary>
public static class BoundedHypertextTransferProtocolPayloadReader
{
    private const int BufferSizeInBytes = 8192;

    /// <summary>
    ///     Reads the body of <paramref name="content" /> as a UTF-8 string, aborting and
    ///     returning <see langword="null" /> as soon as more than <paramref name="maximumBytes" />
    ///     bytes have been received. The unread portion of the response stream is left for
    ///     the caller to dispose alongside the originating <see cref="HttpResponseMessage" />.
    /// </summary>
    /// <param name="content">The HTTP response content to read.</param>
    /// <param name="maximumBytes">The maximum number of bytes that may be buffered.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The decoded payload, or <see langword="null" /> if the body exceeded the budget.</returns>
    public static async Task<string?> ReadAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken)
    {
        using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new byte[BufferSizeInBytes];
        using var memory = new MemoryStream();
        var total = 0;
        while (true)
        {
            var readBuffer = new Memory<byte>(buffer);
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            total += bytesRead;
            if (total > maximumBytes)
            {
                return null;
            }

            var writeBuffer = new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            await memory.WriteAsync(writeBuffer, cancellationToken).ConfigureAwait(false);
        }

        return Encoding.UTF8.GetString(memory.GetBuffer(), 0, (int)memory.Length);
    }
}
