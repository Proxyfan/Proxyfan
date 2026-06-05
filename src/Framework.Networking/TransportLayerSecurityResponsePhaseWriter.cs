using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Encapsulates the response-phase bookkeeping contract for the TLS interceptor: the
///     captured flow is recorded in the traffic store and a
///     <see cref="Domain.Traffic.Events.TrafficFlowCompleted" /> event is published even when
///     <see cref="HypertextTransferProtocolPipeHelpers.WriteResponseAsync" /> throws (which
///     happens, for example, when the client closes the TLS connection immediately after
///     reading a <c>Connection: close</c> response).
/// </summary>
public static class TransportLayerSecurityResponsePhaseWriter
{
    /// <summary>
    ///     Writes the supplied response exchange to the client pipe and records the flow plus
    ///     publishes a <see cref="Domain.Traffic.Events.TrafficFlowCompleted" /> event inside a
    ///     <see langword="finally" /> block so the bookkeeping runs regardless of the write
    ///     outcome.
    /// </summary>
    /// <param name="request">The bundled inputs for the write-and-publish operation.</param>
    /// <param name="cancellationToken">A token that cancels the write operation.</param>
    /// <returns>A task that completes after the bookkeeping has been recorded.</returns>
    public static async Task WriteAndPublishBookkeepingAsync(
        TransportLayerSecurityResponsePhaseWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(request.Writer, request.Exchange, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            request.TrafficStore.Add(request.Flow);
            TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(request.EventBus, request.Flow);
        }
    }
}
