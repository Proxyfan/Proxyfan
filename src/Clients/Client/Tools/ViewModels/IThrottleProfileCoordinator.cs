using Proxyfan.Domain.Throttling;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing boundary for observing and applying throttle profile changes.
/// </summary>
public interface IThrottleProfileCoordinator : IDisposable
{
    /// <summary>
    ///     Raised when the active throttle profile changes. The event is always
    ///     marshalled to the UI thread before subscribers are invoked.
    /// </summary>
    public event ThrottleProfileCoordinatorChanged? Changed;

    /// <summary>
    ///     Gets the currently active throttle profile, or <see langword="null" /> if throttling is disabled.
    /// </summary>
    public ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Applies the provided profile, or disables throttling when <see langword="null" />.
    /// </summary>
    /// <param name="profile">The profile to activate, or <see langword="null" /> to disable throttling.</param>
    public void Apply(ThrottleProfile? profile);
}
