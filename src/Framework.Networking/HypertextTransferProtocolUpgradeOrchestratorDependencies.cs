using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles dependencies for <see cref="HypertextTransferProtocolUpgradeOrchestrator" />.
///     Required by the analyzer parameter-count rule.
/// </summary>
public sealed class HypertextTransferProtocolUpgradeOrchestratorDependencies
{
    /// <summary>
    ///     Gets the publisher used to emit flow lifecycle and response events.
    /// </summary>
    public required HypertextTransferProtocolFlowEventPublisher FlowEventPublisher { get; init; }

    /// <summary>
    ///     Gets the optional DNS override resolver consulted before dialing the upstream host
    ///     so user-configured DNS spoofing entries are honoured during HTTP Upgrade exchanges.
    /// </summary>
    public UpstreamHostResolver? HostResolver { get; init; }

    /// <summary>
    ///     Gets the time source used to timestamp tunnel events.
    /// </summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>
    ///     Gets the store that retains completed traffic flows.
    /// </summary>
    public required ITrafficStore TrafficStore { get; init; }

    /// <summary>
    ///     Gets the optional store that retains captured WebSocket flows.
    /// </summary>
    public IWebSocketStore? WebSocketStore { get; init; }
}
