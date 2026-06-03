using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Handler raised when the traffic-list host distribution changes.
/// </summary>
/// <param name="hostCounts">
///     Snapshot of host-to-flow counts derived from the current flow
///     collection.
/// </param>
public delegate void TrafficListSourceHostsUpdatedHandler(IReadOnlyDictionary<string, int> hostCounts);
