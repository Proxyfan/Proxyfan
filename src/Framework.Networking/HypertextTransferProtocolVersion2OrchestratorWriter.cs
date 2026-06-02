using System;
using System.Buffers;
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
    ///     Serializes <paramref name="frame" /> into a pooled buffer and forwards it to
    ///     <paramref name="destination" />, returning the buffer to
    ///     <see cref="ArrayPool{T}.Shared" /> after the write completes. Returns
    ///     <see langword="false" /> when the peer disposed or aborted the connection.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="frame">The parsed frame to forward verbatim.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     <see langword="true" /> on success; <see langword="false" /> when an I/O error
    ///     prevented the write.
    /// </returns>
    public static async Task<bool> TryForwardFrameAsync(Stream destination, HypertextTransferProtocolVersion2Frame frame, CancellationToken cancellationToken)
    {
        var totalLength = HypertextTransferProtocolVersion2FrameParser.HeaderLength + frame.Header.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            var descriptor = HypertextTransferProtocolVersion2OrchestratorHelpers.BuildDescriptor(frame);
            HypertextTransferProtocolVersion2FrameWriter.WriteFrame(buffer, descriptor, frame.Payload.Span);

            var frameMemory = new ReadOnlyMemory<byte>(buffer, 0, totalLength);
            return await TryWriteFrameAsync(destination, frameMemory, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

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
    public static async Task<bool> TryWriteFrameAsync(Stream destination, ReadOnlyMemory<byte> frameBuffer, CancellationToken cancellationToken)
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
