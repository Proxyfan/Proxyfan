using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object that bundles the inputs needed by the WebSocket upgrade dispatch path
///     inside the HTTP proxy handler. Required to stay within the analyzer's 4-parameter
///     constructor/method limit (ATXCS022) while keeping the upgrade dispatch self-contained.
/// </summary>
public sealed class UpgradeExchangeRequest
{
    /// <summary>
    ///     Gets the client-facing proxy connection used to write the upgrade response and to
    ///     pump the tunneled WebSocket bytes once the upgrade succeeds.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the request as modified by rules, scripting, and breakpoints. This is the
    ///     request whose host header drives upstream connection target selection and whose
    ///     Upgrade/Connection headers are forwarded verbatim.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for the upgrade exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the original request exchange (header bytes + body) used to rebuild the
    ///     upstream-bound request payload.
    /// </summary>
    public required HypertextTransferProtocolProxyRequestExchange RequestExchange { get; init; }
}
