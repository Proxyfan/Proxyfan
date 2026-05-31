using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundled dependencies for <see cref="HypertextTransferProtocolVersion2Orchestrator" />.
///     Wrapped in a parameter object so the orchestrator constructor stays under the
///     analyzer-enforced four-parameter limit (ATXCS022).
/// </summary>
public sealed class HypertextTransferProtocolVersion2OrchestratorDependencies
{
    /// <summary>
    ///     Gets the publisher used to emit flow lifecycle and HTTP-level events.
    /// </summary>
    public required HypertextTransferProtocolFlowEventPublisher FlowEventPublisher { get; init; }

    /// <summary>
    ///     Gets the optional Remote Procedure Call (gRPC) store. When non-null, the
    ///     orchestrator inspects HTTP/2 response headers for an <c>application/grpc</c>
    ///     content type and extracts length-prefixed gRPC messages into a
    ///     <see cref="RemoteProcedureCallFlow" /> for the inspector.
    /// </summary>
    public IRemoteProcedureCallStore? RemoteProcedureCallStore { get; init; }

    /// <summary>
    ///     Gets the wall-clock time source used to timestamp captured gRPC messages.
    ///     Defaults to <see cref="System.TimeProvider.System" /> when the orchestrator is
    ///     constructed without one (covered by the default-initialization in the
    ///     orchestrator constructor).
    /// </summary>
    public System.TimeProvider? TimeProvider { get; init; }

    /// <summary>
    ///     Gets the traffic store that retains completed flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
