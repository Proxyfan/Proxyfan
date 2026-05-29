using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     In-memory stub <see cref="IReverseProxyEngine" /> used to exercise the
///     <see cref="Proxyfan.Client.Tools.ViewModels.ReverseProxySettingsViewModel" />
///     and <see cref="Proxyfan.Domain.Proxy.PeriodicReverseProxyHealthChecker" />
///     without binding real network sockets.
/// </summary>
internal sealed class StubReverseProxyEngine : IReverseProxyEngine
{
    private readonly Dictionary<string, ReverseProxyRouteState> _states = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public event ReverseProxyRouteStatusChanged? StatusChanged;

    /// <summary>
    ///     Gets the number of times <see cref="ProbeAsync" /> was called.
    /// </summary>
    public int ProbeCallCount { get; private set; }

    /// <summary>
    ///     Gets the next status returned by <see cref="ProbeAsync" />.
    /// </summary>
    public ReverseProxyRouteStatus NextProbeStatus { get; set; } = ReverseProxyRouteStatus.Healthy;

    /// <summary>
    ///     Gets or sets a value that, when non-null, causes <see cref="ProbeAsync" /> to throw it.
    ///     Used to verify the periodic checker survives per-route exceptions.
    /// </summary>
    public Exception? NextProbeException { get; set; }

    /// <summary>
    ///     Gets the next return value for <see cref="StartRouteAsync" />.
    /// </summary>
    public bool NextStartResult { get; set; } = true;

    /// <summary>
    ///     Raises the <see cref="StatusChanged" /> event for the supplied identifier and status,
    ///     to let tests simulate engine notifications.
    /// </summary>
    /// <param name="identifier">The route identifier.</param>
    /// <param name="status">The status to publish.</param>
    public void RaiseStatusChanged(string identifier, ReverseProxyRouteStatus status)
    {
        if (_states.TryGetValue(identifier, out var existing))
        {
            _states[identifier] = new ReverseProxyRouteState(existing.Route, status);
        }

        StatusChanged?.Invoke(identifier, status);
    }

    /// <inheritdoc />
    public IReadOnlyList<ReverseProxyRouteState> GetStates()
    {
        return [.. _states.Values];
    }

    /// <inheritdoc />
    public Task<ReverseProxyRouteStatus> ProbeAsync(string identifier, CancellationToken cancellationToken)
    {
        ProbeCallCount++;
        if (NextProbeException is not null)
        {
            throw NextProbeException;
        }

        if (_states.TryGetValue(identifier, out var existing))
        {
            _states[identifier] = new ReverseProxyRouteState(existing.Route, NextProbeStatus);
            StatusChanged?.Invoke(identifier, NextProbeStatus);
        }

        return Task.FromResult(NextProbeStatus);
    }

    /// <inheritdoc />
    public Task<bool> StartRouteAsync(ReverseProxyRoute route, CancellationToken cancellationToken)
    {
        if (NextStartResult)
        {
            _states[route.Identifier] = new ReverseProxyRouteState(route, ReverseProxyRouteStatus.Healthy);
            StatusChanged?.Invoke(route.Identifier, ReverseProxyRouteStatus.Healthy);
        }

        return Task.FromResult(NextStartResult);
    }

    /// <inheritdoc />
    public Task<bool> StopRouteAsync(string identifier, CancellationToken cancellationToken)
    {
        if (_states.TryGetValue(identifier, out var existing))
        {
            _states[identifier] = new ReverseProxyRouteState(existing.Route, ReverseProxyRouteStatus.Stopped);
            StatusChanged?.Invoke(identifier, ReverseProxyRouteStatus.Stopped);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
