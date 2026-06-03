using CommunityToolkit.Mvvm.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight DTO representing one throttle preset shown in the picker. The
///     <see cref="Identifier" /> is a stable, culture-invariant key used for matching against
///     the active preset, while <see cref="DisplayName" /> holds the user-visible (and possibly
///     localized) label.
/// </summary>
public sealed partial class ThrottleProfilePresetViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    ///     Gets the stable culture-invariant identifier of the preset.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" /> with a distinct
    ///     stable identifier and user-visible display name.
    /// </summary>
    /// <param name="identifier">The culture-invariant identifier.</param>
    /// <param name="displayName">The user-visible name.</param>
    public ThrottleProfilePresetViewModel(string identifier, string displayName)
    {
        Identifier = identifier;
        _displayName = displayName;
    }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleProfilePresetViewModel" /> where the
    ///     identifier doubles as the display name (no localization available).
    /// </summary>
    /// <param name="identifier">The culture-invariant identifier, also used as the display name.</param>
    public ThrottleProfilePresetViewModel(string identifier)
        : this(identifier, identifier)
    {
    }
}
