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
    /// <param name="hypertextTransferProtocolHandler">
    ///     Optional HTTP capture handler. When non-null, HTTP-shaped traffic on
    ///     non-TLS routes is fully captured and processed by the rule pipeline.
    /// </param>
    /// <returns>A new listener instance.</returns>
    public static ReverseProxyRouteListener Create(
        ReverseProxyRoute route,
        ILoggerFactory loggerFactory,
        ReverseProxyHypertextTransferProtocolHandler? hypertextTransferProtocolHandler)
    {
        var logger = loggerFactory.CreateLogger<ReverseProxyRouteListener>();
        var listener = new ReverseProxyRouteListener(route, logger, hypertextTransferProtocolHandler);
        return listener;
    }
}
