using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate signature for <see cref="IThrottleCoordinator.Changed" />.
/// </summary>
/// <param name="profile">The new active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleCoordinatorChangedHandler(ThrottleProfile? profile);
