using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing coordinator for reading and applying the active
///     throttle profile without exposing the mutable runtime holder directly to
///     view models.
/// </summary>
public interface IThrottleProfileCoordinator
{
    /// <summary>
    ///     Raised when the active throttle profile changes.
    /// </summary>
    public event ThrottleProfileCoordinatorChangedHandler? Changed;

    /// <summary>
    ///     Gets the currently active profile, or <see langword="null" /> when throttling is disabled.
    /// </summary>
    public ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Applies the supplied profile as the active profile.
    /// </summary>
    /// <param name="profile">Profile to activate, or <see langword="null" /> to disable throttling.</param>
    public void Apply(ThrottleProfile? profile);
}
