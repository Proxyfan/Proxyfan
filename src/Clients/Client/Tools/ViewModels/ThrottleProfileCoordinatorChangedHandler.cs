using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Signature of the <see cref="IThrottleProfileCoordinator.Changed" />
///     notification.
/// </summary>
/// <param name="profile">The new active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleProfileCoordinatorChangedHandler(ThrottleProfile? profile);
