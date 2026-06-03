using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Raised when the active throttle profile changes.
/// </summary>
/// <param name="profile">The active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleProfileCoordinatorChanged(ThrottleProfile? profile);
