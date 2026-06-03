using Proxyfan.Domain.Throttling;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Coordinates UI-facing access to the active throttle profile while keeping
///     <see cref="MutableThrottleProfile" /> behind a presentation boundary.
/// </summary>
public interface IThrottleProfileCoordinator : IDisposable
{
    /// <summary>
    ///     Raised when the active profile changes.
    /// </summary>
    event ThrottleProfileCoordinatorChanged? Changed;

    /// <summary>
    ///     Gets the currently active profile, or <see langword="null" /> when disabled.
    /// </summary>
    ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Applies a profile, or disables throttling when <paramref name="profile" /> is <see langword="null" />.
    /// </summary>
    /// <param name="profile">The profile to apply, or <see langword="null" /> to disable.</param>
    void Apply(ThrottleProfile? profile);
}
