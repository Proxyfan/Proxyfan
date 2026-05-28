using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Drives one direction of a WebSocket tunnel: reads frames from the request's
///     <see cref="WebSocketRelayDirectionRequest.Source" /> via its
///     <see cref="WebSocketRelayDirectionRequest.Relay" />, writes to its
///     <see cref="WebSocketRelayDirectionRequest.Destination" />, and signals the
///     linked cancellation source when the direction terminates so the paired direction
///     can wind down promptly.
/// </summary>
public static class WebSocketRelayDirection
{
    /// <summary>
    ///     Pumps one direction of the WebSocket tunnel until the source closes, the relay
    ///     observes a close frame, the cancellation token fires, or an I/O exception breaks
    ///     the pipe.
    /// </summary>
    /// <param name="request">The relay direction parameters.</param>
    /// <param name="cancellationToken">The token used to abort the relay.</param>
    /// <returns>A task that completes when the direction has terminated.</returns>
    public static async Task RelayAsync(WebSocketRelayDirectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await request.Relay
                .RelayAsync(request.Source, request.Destination, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
        catch (IOException ex)
        {
            _ = ex;
        }
        finally
        {
            await request.LinkedSource.CancelAsync().ConfigureAwait(false);
        }
    }
}
