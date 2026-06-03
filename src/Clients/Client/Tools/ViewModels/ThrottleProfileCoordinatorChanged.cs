using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate signature for the <see cref="IThrottleProfileCoordinator" /> <c>Changed</c> event.
/// </summary>
/// <param name="profile">The new active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleProfileCoordinatorChanged(ThrottleProfile? profile);
