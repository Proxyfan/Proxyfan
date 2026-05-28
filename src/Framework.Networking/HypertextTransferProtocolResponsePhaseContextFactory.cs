using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds <see cref="HypertextTransferProtocolResponsePhaseContext" /> instances from
///     loose handler-side variables so the call-site stays under analyzer constraints (line
///     budget and inline-new prohibition).
/// </summary>
public static class HypertextTransferProtocolResponsePhaseContextFactory
{
    /// <summary>
    ///     Composes a context for the response-phase pipeline.
    /// </summary>
    /// <param name="connection">The owning proxy connection.</param>
    /// <param name="effectiveRequest">The request after rules, scripting, and breakpoints.</param>
    /// <param name="flow">The traffic flow accumulating capture data.</param>
    /// <param name="responseExchange">The upstream response exchange.</param>
    /// <returns>The composed context.</returns>
    public static HypertextTransferProtocolResponsePhaseContext Create(
        IProxyConnection connection,
        HypertextTransferProtocolRequestData effectiveRequest,
        TrafficFlow flow,
        HypertextTransferProtocolProxyResponseExchange responseExchange)
    {
        var context = new HypertextTransferProtocolResponsePhaseContext
        {
            Connection = connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            ResponseExchange = responseExchange,
        };
        return context;
    }
}
