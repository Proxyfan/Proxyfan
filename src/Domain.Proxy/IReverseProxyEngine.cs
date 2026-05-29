using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Abstraction over the reverse proxy engine so presentation-layer components can
///     manage routes without taking a direct dependency on the framework-layer
///     implementation.
/// </summary>
public interface IReverseProxyEngine
{
    /// <summary>
    ///     Raised whenever the engine observes that a route's status has changed
    ///     (start, stop, probe outcome, periodic health check).
    /// </summary>
    event ReverseProxyRouteStatusChanged? StatusChanged;

    /// <summary>
    ///     Gets a snapshot of all routes the engine currently manages, with their statuses.
    /// </summary>
    /// <returns>The current set of route states.</returns>
    IReadOnlyList<ReverseProxyRouteState> GetStates();

    /// <summary>
    ///     Probes the backend for the supplied route and updates its status.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The status after probing.</returns>
    Task<ReverseProxyRouteStatus> ProbeAsync(string identifier, CancellationToken cancellationToken);

    /// <summary>
    ///     Starts the supplied route.
    /// </summary>
    /// <param name="route">The route to start.</param>
    /// <param name="cancellationToken">Cancels start-up.</param>
    /// <returns><see langword="true" /> when the route started successfully.</returns>
    Task<bool> StartRouteAsync(ReverseProxyRoute route, CancellationToken cancellationToken);

    /// <summary>
    ///     Stops the route with the supplied identifier.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <param name="cancellationToken">Cancels shutdown.</param>
    /// <returns><see langword="true" /> when a route was stopped.</returns>
    Task<bool> StopRouteAsync(string identifier, CancellationToken cancellationToken);
}
