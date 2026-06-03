using Proxyfan.Domain.Traffic;
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
    ///     Raised when the traffic list replaces its flow collection (for
    ///     example after importing a HAR file), so derived sibling lists can
    ///     rebuild from the latest snapshot.
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

    private TrafficListFlowsSnapshotProviderHandler? _flowsSnapshotProvider;

    /// <summary>
    ///     Returns a snapshot of the current traffic flows.
    /// </summary>
    /// <returns>The current traffic-flow snapshot, or an empty list when unavailable.</returns>
    public IReadOnlyList<TrafficFlow> GetFlowsSnapshot()
    {
        TrafficListFlowsSnapshotProviderHandler? snapshotProvider = _flowsSnapshotProvider;
        if (snapshotProvider is null)
        {
            return [];
        }

        return snapshotProvider();
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
    ///     Registers a callback used to retrieve the current traffic-flow
    ///     snapshot for sibling view models.
    /// </summary>
    /// <param name="flowsSnapshotProvider">
    ///     The callback that returns the current flow snapshot, or
    ///     <see langword="null" /> to clear any previously-registered callback.
    /// </param>
    public void SetFlowsSnapshotProvider(TrafficListFlowsSnapshotProviderHandler? flowsSnapshotProvider)
    {
        _flowsSnapshotProvider = flowsSnapshotProvider;
    }
}
