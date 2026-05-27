using Proxyfan.Domain.Certificates;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure decision helper for the TLS interceptor: given a target hostname and the SSL
///     proxying list, choose whether to intercept or pass through.
/// </summary>
public static class TransportLayerSecurityStrategySelector
{
    /// <summary>
    ///     Selects the handling strategy for the supplied target.
    /// </summary>
    /// <param name="proxyingList">The SSL proxying list configuration.</param>
    /// <param name="hostname">The CONNECT target hostname.</param>
    /// <returns>The chosen strategy.</returns>
    public static TransportLayerSecurityHandlingStrategy Select(
        ServerNameIndicationProxyingList proxyingList,
        string hostname)
    {
        if (proxyingList.HasMatch(hostname))
        {
            return TransportLayerSecurityHandlingStrategy.InterceptAndInspect;
        }

        return TransportLayerSecurityHandlingStrategy.PassThroughTunnel;
    }
}
