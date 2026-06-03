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
    ///     Raised when the traffic list flow collection changes. The payload
    ///     is the latest collection snapshot.
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

    /// <summary>
    ///     Gets the latest traffic-flow collection snapshot provided by the
    ///     traffic list.
    /// </summary>
    public IReadOnlyList<TrafficFlowViewModel> CurrentFlows { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        CurrentFlows = [];
    }

    /// <summary>
    ///     Publishes a flows-changed notification to subscribers and updates
    ///     the latest flow snapshot.
    /// </summary>
    /// <param name="flows">
    ///     The current flow collection snapshot.
    /// </param>
    public void NotifyFlowsChanged(IReadOnlyList<TrafficFlowViewModel> flows)
    {
        CurrentFlows = flows;
        FlowsChanged?.Invoke(flows);
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
}
