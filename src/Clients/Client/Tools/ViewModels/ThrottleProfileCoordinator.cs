using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Threading;
using System;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Bridges the mutable domain throttle holder into a presentation-facing coordination surface.
/// </summary>
public sealed class ThrottleProfileCoordinator : IThrottleProfileCoordinator, IDisposable
{
    /// <inheritdoc />
    public event ThrottleProfileChanged? ProfileChanged;

    private readonly MutableThrottleProfile _mutableProfile;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfileCoordinator" />.
    /// </summary>
    /// <param name="mutableProfile">Mutable runtime holder for active throttling state.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to apply profile changes on the application UI thread.</param>
    public ThrottleProfileCoordinator(
        MutableThrottleProfile mutableProfile,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _mutableProfile = mutableProfile;
        _userInterfaceScheduler = userInterfaceScheduler;
        _mutableProfile.Changed += OnMutableProfileChanged;
    }

    /// <inheritdoc />
    public void Apply(ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() =>
        {
            if (profile is null)
            {
                _mutableProfile.Disable();
            }
            else
            {
                _mutableProfile.SetProfile(profile);
            }
        });
    }

    /// <inheritdoc />
    public ThrottleProfile? CurrentProfile => _mutableProfile.Profile;

    /// <inheritdoc />
    public void Dispose()
    {
        _mutableProfile.Changed -= OnMutableProfileChanged;
    }

    private void OnMutableProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        ProfileChanged?.Invoke(profile);
    }
}
