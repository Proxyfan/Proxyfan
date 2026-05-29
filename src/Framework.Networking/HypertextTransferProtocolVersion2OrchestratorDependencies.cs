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
    ///     Gets the traffic store that retains completed flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }
}
