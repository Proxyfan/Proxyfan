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
    ///     Raised when the traffic list's host snapshot changes so sibling
    ///     view models can rebuild host-derived state.
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
    ///     Gets the latest host snapshot from the traffic list. Entries are
    ///     repeated per flow so consumers can derive per-host counts.
    /// </summary>
    public IReadOnlyList<string> HostSnapshot { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        HostSnapshot = [];
    }

    /// <summary>
    ///     Publishes a flows-cleared notification to subscribers.
    /// </summary>
    public void NotifyFlowsCleared()
    {
        FlowsCleared?.Invoke();
    }

    /// <summary>
    ///     Publishes the current host snapshot to subscribers.
    /// </summary>
    /// <param name="hostSnapshot">
    ///     Current host snapshot from the traffic list. Duplicates are
    ///     expected for repeated hosts.
    /// </param>
    public void PublishHostSnapshot(IReadOnlyList<string> hostSnapshot)
    {
        UpdateHostSnapshot(hostSnapshot);
        FlowsChanged?.Invoke();
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
    ///     Updates the current host snapshot without notifying subscribers.
    /// </summary>
    /// <param name="hostSnapshot">
    ///     Current host snapshot from the traffic list. Duplicates are
    ///     expected for repeated hosts.
    /// </param>
    public void UpdateHostSnapshot(IReadOnlyList<string> hostSnapshot)
    {
        HostSnapshot = hostSnapshot;
    }
}
