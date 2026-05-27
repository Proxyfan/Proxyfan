using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pumps bytes bidirectionally between two streams (client and backend) until either
///     side closes. Used by the reverse proxy route handler to relay TCP traffic.
/// </summary>
public static class BidirectionalStreamPump
{
    /// <summary>
    ///     Copies <paramref name="left" /> to <paramref name="right" /> and vice versa
    ///     concurrently until either direction completes or the token is cancelled.
    /// </summary>
    /// <param name="left">The first stream.</param>
    /// <param name="right">The second stream.</param>
    /// <param name="bufferSize">Buffer size for each direction.</param>
    /// <param name="cancellationToken">Cancels the pump.</param>
    /// <returns>A task that completes when both directions have stopped.</returns>
    public static async Task PumpAsync(Stream left, Stream right, int bufferSize, CancellationToken cancellationToken)
    {
        var leftToRight = PumpOneDirectionAsync(left, right, bufferSize, cancellationToken);
        var rightToLeft = PumpOneDirectionAsync(right, left, bufferSize, cancellationToken);
        await Task.WhenAny(leftToRight, rightToLeft).ConfigureAwait(false);
    }

    private static async Task PumpOneDirectionAsync(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        var buffer = new byte[bufferSize];
        while (!cancellationToken.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            if (read == 0)
            {
                return;
            }

            try
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }
        }
    }
}
