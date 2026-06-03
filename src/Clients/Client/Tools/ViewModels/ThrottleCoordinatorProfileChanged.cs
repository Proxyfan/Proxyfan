namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Delegate signature for <see cref="IThrottleCoordinator.ProfileChanged" />.
/// </summary>
/// <param name="activeProfileIdentifier">The active profile identifier, or <see langword="null" /> when throttling is disabled.</param>
public delegate void ThrottleCoordinatorProfileChanged(string? activeProfileIdentifier);
