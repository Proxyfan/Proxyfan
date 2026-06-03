namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.FlowsChanged" />
///     notification raised when the traffic list has updated its current flow
///     set and dependent views should rebuild their derived state from the
///     latest snapshot.
/// </summary>
public delegate void TrafficListFlowsChangedHandler();
