using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate signature for throttle coordinator change notifications.
/// </summary>
/// <param name="profile">The active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleProfileCoordinatorChanged(ThrottleProfile? profile);
