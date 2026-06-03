namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowsChanged" />
///     notification raised by the traffic list when its flow collection
///     changes and derived sibling state should be recomputed.
/// </summary>
public delegate void TrafficListFlowsChangedHandler();
