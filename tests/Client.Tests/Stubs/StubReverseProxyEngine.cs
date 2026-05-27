using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     In-memory stub <see cref="IReverseProxyEngine" /> used to exercise the
///     <see cref="Proxyfan.Client.Tools.ViewModels.ReverseProxySettingsViewModel" />
///     without binding real network sockets.
/// </summary>
internal sealed class StubReverseProxyEngine : IReverseProxyEngine
{
    private readonly Dictionary<string, ReverseProxyRouteState> _states = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets the number of times <see cref="ProbeAsync" /> was called.
    /// </summary>
    public int ProbeCallCount { get; private set; }

    /// <summary>
    ///     Gets the next status returned by <see cref="ProbeAsync" />.
    /// </summary>
    public ReverseProxyRouteStatus NextProbeStatus { get; set; } = ReverseProxyRouteStatus.Healthy;

    /// <summary>
    ///     Gets the next return value for <see cref="StartRouteAsync" />.
    /// </summary>
    public bool NextStartResult { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyList<ReverseProxyRouteState> GetStates()
    {
        return [.. _states.Values];
    }

    /// <inheritdoc />
    public Task<ReverseProxyRouteStatus> ProbeAsync(string identifier, CancellationToken cancellationToken)
    {
        ProbeCallCount++;
        if (_states.TryGetValue(identifier, out var existing))
        {
            _states[identifier] = new ReverseProxyRouteState(existing.Route, NextProbeStatus);
        }

        return Task.FromResult(NextProbeStatus);
    }

    /// <inheritdoc />
    public Task<bool> StartRouteAsync(ReverseProxyRoute route, CancellationToken cancellationToken)
    {
        if (NextStartResult)
        {
            _states[route.Identifier] = new ReverseProxyRouteState(route, ReverseProxyRouteStatus.Healthy);
        }

        return Task.FromResult(NextStartResult);
    }

    /// <inheritdoc />
    public Task<bool> StopRouteAsync(string identifier, CancellationToken cancellationToken)
    {
        if (_states.TryGetValue(identifier, out var existing))
        {
            _states[identifier] = new ReverseProxyRouteState(existing.Route, ReverseProxyRouteStatus.Stopped);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
