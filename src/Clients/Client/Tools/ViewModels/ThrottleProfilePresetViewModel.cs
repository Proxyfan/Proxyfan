using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain.Throttling;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight view-model representing one throttle preset shown in the picker.
///     Exposes a stable <see cref="PresetId" /> (English, used for matching against
///     <see cref="ThrottleProfile.Name" />) and a culture-aware
///     <see cref="DisplayName" /> sourced from the client Resources table.
/// </summary>
public sealed partial class ThrottleProfilePresetViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    ///     Gets the stable English identifier of the preset, used for matching
    ///     external profile mutations against the picker independent of UI culture.
    /// </summary>
    public string PresetId { get; }

    /// <summary>
    ///     Gets the underlying throttle profile, or <see langword="null" /> when the preset
    ///     represents "no throttling".
    /// </summary>
    public ThrottleProfile? Profile { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" />.
    /// </summary>
    /// <param name="presetId">The stable English identifier of the preset.</param>
    /// <param name="displayName">The initial localized display name.</param>
    /// <param name="profile">The underlying profile, or <see langword="null" /> to disable throttling.</param>
    public ThrottleProfilePresetViewModel(string presetId, string displayName, ThrottleProfile? profile)
    {
        PresetId = presetId;
        _displayName = displayName;
        Profile = profile;
    }
}
