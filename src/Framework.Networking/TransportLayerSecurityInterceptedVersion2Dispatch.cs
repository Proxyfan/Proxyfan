using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Extracted from <see cref="TransportLayerSecurityInterceptorHandler" /> so the handler
///     stays under the analyzer-enforced 500-line class-size limit. Constructs a per-connection
///     <see cref="HypertextTransferProtocolVersion2Orchestrator" /> and runs it against the
///     supplied decrypted streams.
/// </summary>
public static class TransportLayerSecurityInterceptedVersion2Dispatch
{
    /// <summary>
    ///     Builds a per-connection <see cref="HypertextTransferProtocolVersion2Orchestrator" />
    ///     and runs the bidirectional HTTP/2 relay until both directions close or
    ///     <paramref name="cancellationToken" /> fires.
    /// </summary>
    /// <param name="request">The bundled dispatch request arguments.</param>
    /// <param name="cancellationToken">A token that cancels the orchestration.</param>
    /// <returns>A task that completes when the HTTP/2 connection closes.</returns>
    public static async Task RunAsync(
        TransportLayerSecurityInterceptedVersion2DispatchRequest request,
        CancellationToken cancellationToken)
    {
        var flowPublisher = new HypertextTransferProtocolFlowEventPublisher(request.EventBus);
        var dependencies = new HypertextTransferProtocolVersion2OrchestratorDependencies
        {
            FlowEventPublisher = flowPublisher,
            TrafficStore = request.TrafficStore,
        };
        var orchestrator = new HypertextTransferProtocolVersion2Orchestrator(dependencies);
        var clientEndPoint = request.Connection.RemoteEndPoint?.ToString() ?? "unknown";
        await orchestrator.RunAsync(request.ClientSecureStream, request.ServerSecureStream, clientEndPoint, cancellationToken).ConfigureAwait(false);
    }
}
