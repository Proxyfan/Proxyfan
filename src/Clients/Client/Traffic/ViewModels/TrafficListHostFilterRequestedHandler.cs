namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.HostFilterRequested" />
///     notification raised by the source list when its selection asks the
///     traffic list to narrow visible flows to a given host. An empty string
///     clears the filter.
/// </summary>
/// <param name="host">
///     The host to filter visible flows by, or an empty string to clear.
/// </param>
public delegate void TrafficListHostFilterRequestedHandler(string host);
