using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Factory for <see cref="ReverseProxyRouteListener" /> — created via this helper so the
///     engine can resolve a logger per listener without holding a DI scope.
/// </summary>
public static class ReverseProxyRouteListenerFactory
{
    /// <summary>
    ///     Creates a new <see cref="ReverseProxyRouteListener" /> for the supplied route.
    /// </summary>
    /// <param name="route">The route to listen for.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>A new listener instance.</returns>
    public static ReverseProxyRouteListener Create(ReverseProxyRoute route, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ReverseProxyRouteListener>();
        return new ReverseProxyRouteListener(route, logger);
    }
}
