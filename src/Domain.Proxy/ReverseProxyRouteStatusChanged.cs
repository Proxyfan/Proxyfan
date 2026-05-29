namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Delegate raised whenever the engine observes that a route's
///     <see cref="ReverseProxyRouteStatus" /> has changed.
/// </summary>
/// <param name="identifier">The route identifier whose status changed.</param>
/// <param name="status">The new status.</param>
public delegate void ReverseProxyRouteStatusChanged(string identifier, ReverseProxyRouteStatus status);
