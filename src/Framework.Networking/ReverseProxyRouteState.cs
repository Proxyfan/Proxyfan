using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Snapshot of a route's runtime state inside the <see cref="ReverseProxyEngine" />.
/// </summary>
public sealed class ReverseProxyRouteState
{
    /// <summary>
    ///     Gets the route configuration.
    /// </summary>
    public ReverseProxyRoute Route { get; }

    /// <summary>
    ///     Gets the route's current status.
    /// </summary>
    public ReverseProxyRouteStatus Status { get; }

    /// <summary>
    ///     Initializes a new state snapshot.
    /// </summary>
    /// <param name="route">The route configuration.</param>
    /// <param name="status">The current status.</param>
    public ReverseProxyRouteState(ReverseProxyRoute route, ReverseProxyRouteStatus status)
    {
        Route = route;
        Status = status;
    }
}
