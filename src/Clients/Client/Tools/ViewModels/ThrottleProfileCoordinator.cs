using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Threading;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Bridges the mutable domain throttle holder into a presentation-facing
///     abstraction that view models can observe and command.
/// </summary>
public sealed class ThrottleProfileCoordinator : IThrottleProfileCoordinator
{
    /// <inheritdoc />
    public event ThrottleProfileCoordinatorChangedHandler? Changed;

    private readonly MutableThrottleProfile _mutableProfile;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfileCoordinator" />.
    /// </summary>
    /// <param name="mutableProfile">Mutable domain holder owned by the runtime pipeline.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to publish updates on the UI thread.</param>
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
    public ThrottleProfile? Profile => _mutableProfile.Profile;

    private void OnProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() =>
        {
            Changed?.Invoke(profile);
        });
    }
}
