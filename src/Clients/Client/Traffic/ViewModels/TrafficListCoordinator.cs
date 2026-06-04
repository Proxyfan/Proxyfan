using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Small mediator that decouples the source-list and traffic-list view
///     models. The source list publishes host-filter requests when its
///     selection changes; the traffic list publishes a notification when its
///     flow collection is cleared so any sibling view model (e.g. the source
///     list) can rebuild its derived state. Both view models depend on this
///     abstraction instead of each other, keeping their slices independently
///     constructible and disposable.
/// </summary>
public sealed class TrafficListCoordinator
{
    /// <summary>
    ///     Raised when the traffic list flow collection changes and the source
    ///     list should rebuild from the latest host snapshot.
    /// </summary>
    public event TrafficListFlowsClearedHandler? FlowsChanged;

    /// <summary>
    ///     Raised when the traffic list clears its flow collection. The
    ///     source list rebuilds its host groups in response.
    /// </summary>
    public event TrafficListFlowsClearedHandler? FlowsCleared;

    /// <summary>
    ///     Raised when the source list (or any other publisher) asks the
    ///     traffic list to narrow its visible flows to a specific host. An
    ///     empty string clears the filter.
    /// </summary>
    public event TrafficListHostFilterRequestedHandler? HostFilterRequested;

    private readonly Dictionary<string, int> _sourceHosts;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        var sourceHosts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _sourceHosts = sourceHosts;
    }

    /// <summary>
    ///     Returns a copy of the current host-to-count snapshot derived from
    ///     the traffic list's current flow collection.
    /// </summary>
    /// <returns>A host-to-count snapshot copy.</returns>
    public Dictionary<string, int> GetSourceHostsSnapshot()
    {
        var snapshot = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _sourceHosts)
        {
            snapshot[pair.Key] = pair.Value;
        }

        return snapshot;
    }

    /// <summary>
    ///     Publishes a flows-changed notification to subscribers.
    /// </summary>
    public void NotifyFlowsChanged()
    {
        FlowsChanged?.Invoke();
    }

    /// <summary>
    ///     Publishes a flows-cleared notification to subscribers.
    /// </summary>
    public void NotifyFlowsCleared()
    {
        FlowsCleared?.Invoke();
    }

    /// <summary>
    ///     Publishes a host-filter request to subscribers.
    /// </summary>
    /// <param name="host">
    ///     The host to narrow the visible flows to, or an empty string to
    ///     clear the filter. <see langword="null" /> is normalized to an
    ///     empty string.
    /// </param>
    public void RequestHostFilter(string? host)
    {
        HostFilterRequested?.Invoke(host ?? string.Empty);
    }

    /// <summary>
    ///     Recomputes the host snapshot from the provided flow collection.
    /// </summary>
    /// <param name="flows">The current traffic-list flow collection.</param>
    public void UpdateSourceHosts(ObservableCollection<TrafficFlowViewModel> flows)
    {
        _sourceHosts.Clear();

        foreach (var flow in flows)
        {
            var host = flow.Host;
            if (_sourceHosts.TryGetValue(host, out var count))
            {
                _sourceHosts[host] = count + 1;
                continue;
            }

            _sourceHosts[host] = 1;
        }
    }
}
