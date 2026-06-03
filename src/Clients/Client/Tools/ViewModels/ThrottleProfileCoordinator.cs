using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Threading;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Coordinates UI-facing throttle state with the mutable runtime throttle profile holder.
/// </summary>
public sealed class ThrottleProfileCoordinator : IThrottleProfileCoordinator
{
    /// <inheritdoc />
    public event ThrottleProfileCoordinatorChanged? Changed;

    private readonly MutableThrottleProfile _mutableProfile;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfileCoordinator" />.
    /// </summary>
    /// <param name="mutableProfile">The mutable runtime throttle holder.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal notifications and apply operations to the UI thread.</param>
    public ThrottleProfileCoordinator(
        MutableThrottleProfile mutableProfile,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _mutableProfile = mutableProfile;
        _userInterfaceScheduler = userInterfaceScheduler;
        _mutableProfile.Changed += OnProfileChanged;
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
    public void Dispose()
    {
        _mutableProfile.Changed -= OnProfileChanged;
    }

    /// <inheritdoc />
    public ThrottleProfile? Profile => _mutableProfile.Profile;

    private void OnProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() => Changed?.Invoke(profile));
    }
}
