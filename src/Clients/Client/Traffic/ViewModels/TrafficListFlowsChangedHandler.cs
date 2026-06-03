using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowsChanged" />
///     notification raised by the traffic list whenever its flow collection
///     changes.
/// </summary>
/// <param name="flows">
///     The current traffic-flow view-model collection snapshot.
/// </param>
public delegate void TrafficListFlowsChangedHandler(IReadOnlyList<TrafficFlowViewModel> flows);
