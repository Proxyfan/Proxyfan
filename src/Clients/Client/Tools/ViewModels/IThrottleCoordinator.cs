using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Presentation-facing abstraction over the active throttle state. Exposes immutable
///     profile snapshots and applies preset changes on behalf of the UI, keeping
///     <see cref="MutableThrottleProfile" /> behind this boundary.
/// </summary>
public interface IThrottleCoordinator
{
    /// <summary>
    ///     Raised after the active profile changes. Listeners receive the new profile,
    ///     or <see langword="null" /> when throttling is disabled.
    /// </summary>
    event ThrottleCoordinatorChangedHandler? Changed;

    /// <summary>
    ///     Gets the currently active throttle profile, or <see langword="null" /> if throttling is disabled.
    /// </summary>
    ThrottleProfile? ActiveProfile { get; }

    /// <summary>
    ///     Applies the supplied profile as the active throttle profile, or disables
    ///     throttling when <paramref name="profile" /> is <see langword="null" />.
    /// </summary>
    /// <param name="profile">The profile to activate, or <see langword="null" /> to disable throttling.</param>
    void Apply(ThrottleProfile? profile);
}
