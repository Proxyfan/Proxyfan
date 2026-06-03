using System.Collections.Generic;

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
    ///     Raised when the traffic list's flow collection changes and any
    ///     derived sibling state should be recomputed from the current flow
    ///     snapshot.
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

    private IReadOnlyList<string> _sourceHostsSnapshot;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        _sourceHostsSnapshot = [];
    }

    /// <summary>
    ///     Returns the latest ordered host snapshot derived from the traffic
    ///     list's current flows.
    /// </summary>
    /// <returns>The current ordered host snapshot.</returns>
    public IReadOnlyList<string> GetSourceHostsSnapshot()
    {
        return _sourceHostsSnapshot;
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
    ///     Replaces the latest ordered host snapshot derived from the traffic
    ///     list's current flows.
    /// </summary>
    /// <param name="sourceHosts">The ordered hosts from the current flow snapshot.</param>
    public void SetSourceHostsSnapshot(IReadOnlyList<string> sourceHosts)
    {
        _sourceHostsSnapshot = sourceHosts;
    }
}
