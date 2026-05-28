using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object bundling everything required to execute a TLS-intercepted (wss://)
///     WebSocket upgrade exchange. Required to keep
///     <see cref="TransportLayerSecurityInterceptedUpgradeHandler.HandleAsync" /> within
///     the analyzer's four-parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedUpgradeRequest
{
    /// <summary>
    ///     Gets the loop context (pipes + streams).
    /// </summary>
    public required TransportLayerSecurityInterceptedLoopContext Context { get; init; }

    /// <summary>
    ///     Gets the request after rules/scripting/breakpoint modifications.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for this exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the original request exchange (headers + body) that triggered the upgrade.
    /// </summary>
    public required HypertextTransferProtocolProxyRequestExchange RequestExchange { get; init; }
}
