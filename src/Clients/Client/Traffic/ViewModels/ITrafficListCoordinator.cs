namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Mediates cross-view-model coordination between the source list and the
///     traffic list slices so that neither view model needs to hold a direct
///     reference to the other. Concretely it carries the active host filter
///     selection (published by <see cref="SourceListViewModel" />, observed by
///     <see cref="TrafficListViewModel" />) and a flows-reset signal (raised by
///     <see cref="TrafficListViewModel" />, observed by
///     <see cref="SourceListViewModel" />).
/// </summary>
public interface ITrafficListCoordinator
{
    /// <summary>
    ///     Raised when the traffic list's underlying flows collection has been
    ///     wholesale replaced (cleared or reloaded), signalling that any derived
    ///     projections such as the source-list groups should rebuild.
    /// </summary>
    event TrafficListCoordinatorHandler? FlowsReset;

    /// <summary>
    ///     Raised whenever <see cref="HostFilter" /> has been replaced with a
    ///     different value.
    /// </summary>
    event TrafficListCoordinatorHandler? HostFilterChanged;

    /// <summary>
    ///     Gets or sets the currently selected host filter, or an empty string
    ///     when no host restriction is in effect.
    /// </summary>
    string HostFilter { get; set; }

    /// <summary>
    ///     Notifies subscribers that the traffic list's flows collection has
    ///     been wholesale replaced.
    /// </summary>
    void NotifyFlowsReset();
}
