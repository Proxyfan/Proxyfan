using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static write helper used by <see cref="HypertextTransferProtocolVersion2Orchestrator" />
///     to forward a single frame buffer to the destination stream while gracefully handling
///     the peer disposing or aborting the underlying TCP connection mid-write. Lives in its
///     own type so the analyzer's static-in-non-static-class rule (ATXCS011) is satisfied.
/// </summary>
public static class HypertextTransferProtocolVersion2OrchestratorWriter
{
    /// <summary>
    ///     Writes <paramref name="frameBuffer" /> to <paramref name="destination" /> and
    ///     flushes. Returns <see langword="false" /> when the peer disposed or aborted the
    ///     connection so the caller can break out of its pump loop without throwing.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="frameBuffer">The frame bytes (header + payload).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     <see langword="true" /> on success; <see langword="false" /> when an I/O error
    ///     prevented the write.
    /// </returns>
    public static async Task<bool> TryWriteFrameAsync(Stream destination, byte[] frameBuffer, CancellationToken cancellationToken)
    {
        try
        {
            await destination.WriteAsync(frameBuffer, cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (IOException ex)
        {
            _ = ex;
            return false;
        }
        catch (ObjectDisposedException ex)
        {
            _ = ex;
            return false;
        }
    }
}
