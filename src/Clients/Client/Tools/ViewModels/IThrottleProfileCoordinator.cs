using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing boundary for observing and applying throttle-profile changes
///     without exposing the live mutable domain holder to view models.
/// </summary>
public interface IThrottleProfileCoordinator
{
    /// <summary>
    ///     Raised when the active throttle profile changes.
    /// </summary>
    event ThrottleProfileChanged ProfileChanged;

    /// <summary>
    ///     Gets the currently active throttle profile, or <see langword="null" /> when disabled.
    /// </summary>
    ThrottleProfile? CurrentProfile { get; }

    /// <summary>
    ///     Applies the supplied profile, or disables throttling when <paramref name="profile" /> is <see langword="null" />.
    /// </summary>
    /// <param name="profile">Profile to apply, or <see langword="null" /> to disable throttling.</param>
    void Apply(ThrottleProfile? profile);
}
