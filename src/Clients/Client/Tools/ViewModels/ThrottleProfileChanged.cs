using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate signature for throttle-profile change notifications exposed to presentation code.
/// </summary>
/// <param name="profile">The active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleProfileChanged(ThrottleProfile? profile);
