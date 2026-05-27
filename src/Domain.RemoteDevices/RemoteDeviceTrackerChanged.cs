namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Delegate used by <see cref="RemoteDeviceTracker" /> to notify subscribers that the
///     tracked device collection or one of its members has changed.
/// </summary>
/// <param name="sender">The tracker raising the event.</param>
public delegate void RemoteDeviceTrackerChanged(RemoteDeviceTracker sender);
