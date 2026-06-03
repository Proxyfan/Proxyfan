using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     Signature used by <see cref="TrafficListCoordinator" /> to retrieve
///     a snapshot of the current flow collection.
/// </summary>
/// <returns>The current traffic-flow snapshot.</returns>
public delegate IReadOnlyList<TrafficFlow> TrafficListFlowsSnapshotProviderHandler();
