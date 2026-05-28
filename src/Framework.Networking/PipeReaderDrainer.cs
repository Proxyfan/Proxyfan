using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Helpers for draining whatever bytes a <see cref="PipeReader" /> has already buffered without
///     performing any additional I/O. Used by the upgrade pipeline to capture bytes the reader
///     prefetched past the parsed HTTP headers so they can be replayed into the subsequent tunnel
///     rather than being discarded with the reader.
/// </summary>
public static class PipeReaderDrainer
{
    /// <summary>
    ///     Drains any bytes currently buffered in <paramref name="reader" /> without waiting for
    ///     additional bytes from the underlying source. Uses
    ///     <see cref="PipeReader.CancelPendingRead" /> followed by
    ///     <see cref="PipeReader.ReadAsync(CancellationToken)" /> so the call returns immediately
    ///     with whatever is currently buffered, even when the previous reader marked all bytes as
    ///     examined.
    /// </summary>
    /// <param name="reader">The pipe reader to drain.</param>
    /// <param name="cancellationToken">A token that cancels the drain operation.</param>
    /// <returns>The bytes buffered in <paramref name="reader" /> at the time of the call.</returns>
    public static async ValueTask<byte[]> DrainBufferedBytesAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        reader.CancelPendingRead();
        var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        if (result.Buffer.IsEmpty)
        {
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            return [];
        }

        var prefetched = result.Buffer.ToArray();
        reader.AdvanceTo(result.Buffer.End);
        return prefetched;
    }
}
