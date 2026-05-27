namespace Proxyfan.Domain.Throttling;

/// <summary>
///     Mutable runtime holder for the currently active <see cref="ThrottleProfile" />. The proxy
///     pipeline reads from this holder directly; the UI writes to it when the user picks a new preset.
/// </summary>
public sealed class MutableThrottleProfile
{
    /// <summary>
    ///     Raised after <see cref="Profile" /> has been replaced. Listeners receive the new profile.
    /// </summary>
    public event MutableThrottleProfileChanged? Changed;

    /// <summary>
    ///     Gets the currently active throttle profile, or <see langword="null" /> if throttling is disabled.
    /// </summary>
    public ThrottleProfile? Profile { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="MutableThrottleProfile" /> with throttling disabled.
    /// </summary>
    public MutableThrottleProfile()
    {
        Profile = null;
    }

    /// <summary>
    ///     Initializes a new <see cref="MutableThrottleProfile" /> seeded with the supplied profile.
    /// </summary>
    /// <param name="initialProfile">The profile to begin with, or <see langword="null" /> to start disabled.</param>
    public MutableThrottleProfile(ThrottleProfile? initialProfile)
    {
        Profile = initialProfile;
    }

    /// <summary>
    ///     Disables throttling. No-op when no profile is currently active.
    /// </summary>
    public void Disable()
    {
        if (Profile is null)
        {
            return;
        }

        Profile = null;
        Changed?.Invoke(this, null);
    }

    /// <summary>
    ///     Sets the active throttle profile. Subscribers are notified after the change.
    /// </summary>
    /// <param name="profile">The new profile to activate. Must not be <see langword="null" />.</param>
    public void SetProfile(ThrottleProfile profile)
    {
        if (ReferenceEquals(Profile, profile))
        {
            return;
        }

        Profile = profile;
        Changed?.Invoke(this, profile);
    }
}
