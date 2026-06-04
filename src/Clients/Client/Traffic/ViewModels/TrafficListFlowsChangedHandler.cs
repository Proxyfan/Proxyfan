namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowsChanged" />
///     notification raised by the traffic list when its flow collection
///     changes and dependent projections should rebuild.
/// </summary>
public delegate void TrafficListFlowsChangedHandler();
