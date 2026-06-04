using System.Collections.Generic;
using System.Threading;

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
    ///     Raised when the traffic list flow collection changes and dependent
    ///     views should refresh any derived projections.
    /// </summary>
    public event TrafficListFlowsChangedHandler? FlowsChanged;

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

    private readonly List<string> _sourceHostsSnapshot;
    private readonly Lock _syncRoot;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        var sourceHostsSnapshot = new List<string>();
        var syncRoot = new Lock();
        _sourceHostsSnapshot = sourceHostsSnapshot;
        _syncRoot = syncRoot;
    }

    /// <summary>
    ///     Gets a copy of the current source-host snapshot. One entry exists
    ///     per flow, so duplicate host values represent host counts.
    /// </summary>
    /// <returns>A copy of the source-host snapshot.</returns>
    public IReadOnlyList<string> GetSourceHostsSnapshot()
    {
        lock (_syncRoot)
        {
            var hosts = new List<string>();
            foreach (var host in _sourceHostsSnapshot)
            {
                hosts.Add(host);
            }

            return hosts;
        }
    }

    /// <summary>
    ///     Publishes a flow-collection-changed notification to subscribers.
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
    ///     Replaces the current source-host snapshot with the supplied hosts.
    ///     One entry should be supplied per flow.
    /// </summary>
    /// <param name="hosts">
    ///     The source hosts to snapshot, where each entry represents one flow.
    /// </param>
    public void UpdateSourceHostsSnapshot(IReadOnlyList<string> hosts)
    {
        lock (_syncRoot)
        {
            _sourceHostsSnapshot.Clear();

            foreach (var host in hosts)
            {
                _sourceHostsSnapshot.Add(host);
            }
        }
    }
}
