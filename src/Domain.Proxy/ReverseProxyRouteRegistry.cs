using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     In-memory registry of user-configured reverse proxy routes. Add/Remove operations
///     mutate a snapshot list; the engine consumes the snapshot when starting routes.
/// </summary>
public sealed class ReverseProxyRouteRegistry
{
    private readonly Lock _gate;
    private readonly List<ReverseProxyRoute> _routes;

    /// <summary>
    ///     Gets a snapshot of the routes currently registered.
    /// </summary>
    public IReadOnlyList<ReverseProxyRoute> Routes
    {
        get
        {
            lock (_gate)
            {
                return [.. _routes];
            }
        }
    }

    /// <summary>
    ///     Initializes a new empty registry.
    /// </summary>
    public ReverseProxyRouteRegistry()
    {
        var gate = new Lock();
        _gate = gate;
        _routes = [];
    }

    /// <summary>
    ///     Adds a route. Returns false if a route with the same identifier already exists or
    ///     if its listen port conflicts with an existing route.
    /// </summary>
    /// <param name="route">The route to add.</param>
    /// <returns><see langword="true" /> when the route was added.</returns>
    public bool CanAdd(ReverseProxyRoute route)
    {
        lock (_gate)
        {
            foreach (var existing in _routes)
            {
                if (string.Equals(existing.Identifier, route.Identifier, StringComparison.Ordinal))
                {
                    return false;
                }

                if (existing.ListenPort == route.ListenPort)
                {
                    return false;
                }
            }

            _routes.Add(route);
            return true;
        }
    }

    /// <summary>
    ///     Removes the route with the supplied identifier.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <returns><see langword="true" /> when a route was removed.</returns>
    public bool HasRemoved(string identifier)
    {
        lock (_gate)
        {
            for (var index = 0; index < _routes.Count; index++)
            {
                if (string.Equals(_routes[index].Identifier, identifier, StringComparison.Ordinal))
                {
                    _routes.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }
    }
}
