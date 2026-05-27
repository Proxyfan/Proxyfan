namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Delegate signature for <see cref="MutableThrottleProfile.Changed" />.
/// </summary>
/// <param name="sender">The holder whose profile changed.</param>
/// <param name="profile">The new active profile, or <see langword="null" /> when throttling is disabled.</param>
public delegate void MutableThrottleProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile);
