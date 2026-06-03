using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Presentation.Theming;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only DTO representing one theme option shown in the picker.
///     The display name is resolved through the active <see cref="Proxyfan.Presentation.Localization.LocalizationService" />
///     so the picker reflects Proxyfan's locale resolution and runtime language switching.
/// </summary>
public sealed partial class ThemeOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    ///     Gets the resource key used to resolve the localized <see cref="DisplayName" />.
    /// </summary>
    public string ResourceKey { get; }

    /// <summary>
    ///     Gets the theme enum value applied when this option is selected.
    /// </summary>
    public AppTheme Theme { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThemeOptionViewModel" />.
    /// </summary>
    /// <param name="resourceKey">The resource key used to resolve the localized display name.</param>
    /// <param name="displayName">The initial localized display name.</param>
    /// <param name="theme">The theme variant this option represents.</param>
    public ThemeOptionViewModel(string resourceKey, string displayName, AppTheme theme)
    {
        ResourceKey = resourceKey;
        _displayName = displayName;
        Theme = theme;
    }
}
