namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowsChanged" />
///     notification raised by the traffic list when its flow collection has
///     changed and source-host groups should be rebuilt from the current set.
/// </summary>
public delegate void TrafficListFlowsChangedHandler();
