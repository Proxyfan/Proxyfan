using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowHostsChanged" />
///     notification raised by the traffic list whenever its flow collection
///     changes and a current host snapshot is available.
/// </summary>
/// <param name="hosts">
///     Snapshot of flow hosts (one entry per flow) used by subscribers to
///     rebuild host-group counts from the current traffic state.
/// </param>
public delegate void TrafficListFlowHostsChangedHandler(IReadOnlyList<string> hosts);
