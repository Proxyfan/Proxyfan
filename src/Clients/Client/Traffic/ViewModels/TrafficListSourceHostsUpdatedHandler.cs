using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature of the <see cref="TrafficListCoordinator.SourceHostsUpdated" />
///     notification raised by the traffic list when it publishes a complete
///     host snapshot for source-list recomputation.
/// </summary>
/// <param name="hosts">The current flow hosts.</param>
public delegate void TrafficListSourceHostsUpdatedHandler(IReadOnlyList<string> hosts);
