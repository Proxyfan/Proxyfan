using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only DTO representing one throttle preset shown in the picker.
/// </summary>
public sealed class ThrottleProfilePresetViewModel
{
    /// <summary>
    ///     Gets the user-visible display name of the preset.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the underlying throttle profile, or <see langword="null" /> when the preset
    ///     represents "no throttling".
    /// </summary>
    public ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" />.
    /// </summary>
    /// <param name="displayName">The user-visible name.</param>
    /// <param name="profile">The underlying profile, or <see langword="null" /> to disable throttling.</param>
    public ThrottleProfilePresetViewModel(string displayName, ThrottleProfile? profile)
    {
        DisplayName = displayName;
        Profile = profile;
    }
}
