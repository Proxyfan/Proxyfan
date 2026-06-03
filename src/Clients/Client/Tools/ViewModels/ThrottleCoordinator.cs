using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Default <see cref="IThrottleCoordinator" /> implementation. Wraps a
///     <see cref="MutableThrottleProfile" /> and forwards preset-apply requests to it,
///     keeping the mutable domain object behind the presentation boundary.
/// </summary>
public sealed class ThrottleCoordinator : IThrottleCoordinator
{
    /// <inheritdoc />
    public event ThrottleCoordinatorChangedHandler? Changed;

    private readonly MutableThrottleProfile _mutableProfile;

    /// <summary>
    ///     Initializes a new <see cref="ThrottleCoordinator" /> bound to the supplied holder.
    /// </summary>
    /// <param name="mutableProfile">The mutable runtime holder that backs the coordinator.</param>
    public ThrottleCoordinator(MutableThrottleProfile mutableProfile)
    {
        _mutableProfile = mutableProfile;
        _mutableProfile.Changed += OnMutableProfileChanged;
    }

    /// <inheritdoc />
    public ThrottleProfile? ActiveProfile => _mutableProfile.Profile;

    /// <inheritdoc />
    public void Apply(ThrottleProfile? profile)
    {
        if (profile is null)
        {
            _mutableProfile.Disable();
        }
        else
        {
            _mutableProfile.SetProfile(profile);
        }
    }

    private void OnMutableProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        Changed?.Invoke(profile);
    }
}
