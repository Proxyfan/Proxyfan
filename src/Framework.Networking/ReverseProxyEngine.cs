using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Manages the lifecycle of multiple reverse proxy routes: starts and stops listeners
///     and probes each backend's health on demand.
/// </summary>
public sealed partial class ReverseProxyEngine : IAsyncDisposable
{
    private readonly Lock _gate;
    private readonly IBackendHealthProbe _healthProbe;
    private readonly Dictionary<string, ReverseProxyRouteListener> _listeners;
    private readonly ILogger<ReverseProxyEngine> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, ReverseProxyRouteStatus> _statuses;
    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxyEngine" />.
    /// </summary>
    /// <param name="healthProbe">The probe used to check backend availability.</param>
    /// <param name="loggerFactory">Logger factory for per-listener loggers.</param>
    /// <param name="logger">Engine logger.</param>
    public ReverseProxyEngine(
        IBackendHealthProbe healthProbe,
        ILoggerFactory loggerFactory,
        ILogger<ReverseProxyEngine> logger)
    {
        _healthProbe = healthProbe;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _listeners = [];
        _statuses = [];
        var gate = new Lock();
        _gate = gate;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        List<ReverseProxyRouteListener> snapshot;
        lock (_gate)
        {
            snapshot = [.. _listeners.Values];
            _listeners.Clear();
        }

        foreach (var listener in snapshot)
        {
            try
            {
                await listener.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ = ex;
            }

            listener.Dispose();
        }
    }

    /// <summary>
    ///     Gets a snapshot of all routes the engine currently manages and their statuses.
    /// </summary>
    /// <returns>A list of route states.</returns>
    public IReadOnlyList<ReverseProxyRouteState> GetStates()
    {
        lock (_gate)
        {
            var snapshot = new List<ReverseProxyRouteState>(_listeners.Count);
            foreach (var pair in _listeners)
            {
                var status = _statuses.GetValueOrDefault(pair.Key, ReverseProxyRouteStatus.Stopped);
                var state = new ReverseProxyRouteState(pair.Value.GetRoute(), status);
                snapshot.Add(state);
            }

            return snapshot;
        }
    }

    /// <summary>
    ///     Probes a route's backend and updates its status. Routes that have not been started
    ///     are ignored.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The status after probing, or <see cref="ReverseProxyRouteStatus.Stopped" />.</returns>
    public async Task<ReverseProxyRouteStatus> ProbeAsync(string identifier, CancellationToken cancellationToken)
    {
        ReverseProxyRouteListener? listener;
        lock (_gate)
        {
            if (!_listeners.TryGetValue(identifier, out listener))
            {
                return ReverseProxyRouteStatus.Stopped;
            }
        }

        var route = listener.GetRoute();
        var healthy = await _healthProbe.ProbeAsync(route.BackendHost, route.BackendPort, cancellationToken).ConfigureAwait(false);
        ReverseProxyRouteStatus status;
        if (healthy)
        {
            status = ReverseProxyRouteStatus.Healthy;
        }
        else
        {
            status = ReverseProxyRouteStatus.Unhealthy;
        }

        lock (_gate)
        {
            _statuses[identifier] = status;
        }

        return status;
    }

    /// <summary>
    ///     Starts the supplied route.
    /// </summary>
    /// <param name="route">The route to start.</param>
    /// <param name="cancellationToken">Cancels start-up.</param>
    /// <returns>True when the route started successfully.</returns>
    public async Task<bool> StartRouteAsync(ReverseProxyRoute route, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_listeners.ContainsKey(route.Identifier))
            {
                return false;
            }
        }

        var listener = ReverseProxyRouteListenerFactory.Create(route, _loggerFactory);
        try
        {
            await listener.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ProxyBindException ex)
        {
            LogStartFailed(ex, route.Identifier);
            lock (_gate)
            {
                _statuses[route.Identifier] = ReverseProxyRouteStatus.Faulted;
            }

            listener.Dispose();
            return false;
        }

        lock (_gate)
        {
            _listeners[route.Identifier] = listener;
            _statuses[route.Identifier] = ReverseProxyRouteStatus.Healthy;
        }

        return true;
    }

    /// <summary>
    ///     Stops the route with the supplied identifier.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <param name="cancellationToken">Cancels shutdown.</param>
    /// <returns>True when a route was stopped.</returns>
    public async Task<bool> StopRouteAsync(string identifier, CancellationToken cancellationToken)
    {
        ReverseProxyRouteListener? listener;
        lock (_gate)
        {
            if (!_listeners.Remove(identifier, out listener))
            {
                return false;
            }

            _statuses[identifier] = ReverseProxyRouteStatus.Stopped;
        }

        await listener.StopAsync(cancellationToken).ConfigureAwait(false);
        listener.Dispose();
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Reverse proxy engine failed to start route {Identifier}")]
    private partial void LogStartFailed(Exception ex, string identifier);
}
