using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds <see cref="UpgradeExchangeRequest" /> instances from the loose handler-side
///     variables that the WebSocket upgrade dispatch path needs. Wraps the required init
///     property setters so the call site stays a single line and avoids the inline-new
///     prohibition (ATXCS058) and 4-parameter constructor limit (ATXCS022).
/// </summary>
public static class UpgradeExchangeRequestFactory
{
    /// <summary>
    ///     Composes an <see cref="UpgradeExchangeRequest" /> from the supplied components.
    /// </summary>
    /// <param name="connection">The client-facing proxy connection.</param>
    /// <param name="effectiveRequest">The request after rules/scripting modifications.</param>
    /// <param name="flow">The accumulating traffic flow.</param>
    /// <param name="requestExchange">The original request exchange.</param>
    /// <returns>The composed upgrade exchange request.</returns>
    public static UpgradeExchangeRequest Create(
        IProxyConnection connection,
        HypertextTransferProtocolRequestData effectiveRequest,
        TrafficFlow flow,
        HypertextTransferProtocolProxyRequestExchange requestExchange)
    {
        var request = new UpgradeExchangeRequest
        {
            Connection = connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            RequestExchange = requestExchange,
        };
        return request;
    }
}
