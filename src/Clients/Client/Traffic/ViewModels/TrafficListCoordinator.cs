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
    private readonly List<string> _sourceHostsSnapshot;

    /// <summary>
    ///     Initializes a new <see cref="TrafficListCoordinator" />.
    /// </summary>
    public TrafficListCoordinator()
    {
        var sourceHostsGate = new Lock();
        _sourceHostsGate = sourceHostsGate;
        var sourceHostsSnapshot = new List<string>();
        _sourceHostsSnapshot = sourceHostsSnapshot;
    }

    /// <summary>
    ///     Returns a point-in-time snapshot of source hosts, one entry per flow.
    /// </summary>
    /// <returns>
    ///     A copy of the current source-host snapshot.
    /// </returns>
    public IReadOnlyList<string> GetSourceHostsSnapshot()
    {
        lock (_sourceHostsGate)
        {
            if (_sourceHostsSnapshot.Count == 0)
            {
                return [];
            }

            var snapshot = new string[_sourceHostsSnapshot.Count];
            _sourceHostsSnapshot.CopyTo(snapshot);
            return snapshot;
        }
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
    ///     Replaces the source-host snapshot consumed by sibling view models.
    /// </summary>
    /// <param name="hosts">
    ///     The source hosts to persist as the latest snapshot.
    /// </param>
    public void SetSourceHostsSnapshot(IReadOnlyList<string> hosts)
    {
        lock (_sourceHostsGate)
        {
            _sourceHostsSnapshot.Clear();
            foreach (var host in hosts)
            {
                _sourceHostsSnapshot.Add(host);
            }
        }
    }
}
