using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight DTO representing one throttle preset shown in the picker. The
///     <see cref="Identifier" /> is a stable, culture-invariant key used for
///     matching against the active <see cref="ThrottleProfile" />, while
///     <see cref="DisplayName" /> holds the user-visible (and possibly localized)
///     label.
/// </summary>
public sealed partial class ThrottleProfilePresetViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    ///     Gets the stable culture-invariant identifier of the preset, matching
    ///     <see cref="ThrottleProfile.Name" /> for the equivalent profile (or
    ///     <c>"Off"</c> for the "disable throttling" entry).
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets the underlying throttle profile, or <see langword="null" /> when the preset
    ///     represents "no throttling".
    /// </summary>
    public ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" /> with a distinct
    ///     stable identifier and user-visible display name.
    /// </summary>
    /// <param name="identifier">The culture-invariant identifier.</param>
    /// <param name="displayName">The user-visible name.</param>
    /// <param name="profile">The underlying profile, or <see langword="null" /> to disable throttling.</param>
    public ThrottleProfilePresetViewModel(string identifier, string displayName, ThrottleProfile? profile)
    {
        Identifier = identifier;
        _displayName = displayName;
        Profile = profile;
    }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" /> where the
    ///     identifier doubles as the display name (no localization available).
    /// </summary>
    /// <param name="identifier">The culture-invariant identifier, also used as the display name.</param>
    /// <param name="profile">The underlying profile, or <see langword="null" /> to disable throttling.</param>
    public ThrottleProfilePresetViewModel(string identifier, ThrottleProfile? profile)
        : this(identifier, identifier, profile)
    {
    }
}
