using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    ///     Raised when the traffic list flow collection changes. The source
    ///     list rebuilds its host groups in response.
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
    private readonly Lock _sourceHostsGate;
    private IReadOnlyList<string> _sourceHostsSnapshot;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        var sourceHostsGate = new Lock();
        _sourceHostsGate = sourceHostsGate;
        _sourceHostsSnapshot = [];
    }

    /// <summary>
    ///     Returns a snapshot of host values derived from the current flow
    ///     collection. The snapshot may contain duplicate hosts.
    /// </summary>
    /// <returns>The host snapshot used by source-list rebuild.</returns>
    public IReadOnlyList<string> GetSourceHostsSnapshot()
    {
        lock (_sourceHostsGate)
        {
            return _sourceHostsSnapshot;
        }
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
    ///     Recomputes the source-host snapshot from the supplied flow
    ///     collection so subscribers can rebuild host groups.
    /// </summary>
    /// <param name="flows">The current traffic-list flow collection.</param>
    public void UpdateSourceHosts(ObservableCollection<TrafficFlowViewModel> flows)
    {
        var hosts = new List<string>(flows.Count);
        foreach (var flow in flows)
        {
            var request = flow.Request;
            if (request is null)
            {
                hosts.Add("(tunnel)");
                continue;
            }

            var host = request.Headers.Get("Host");
            if (string.IsNullOrWhiteSpace(host))
            {
                hosts.Add(request.RequestUri.Host);
                continue;
            }

            hosts.Add(host);
        }

        lock (_sourceHostsGate)
        {
            _sourceHostsSnapshot = hosts;
        }
    }
}
