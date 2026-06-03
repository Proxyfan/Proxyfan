using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Presentation.Theming;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Observable view model representing one theme option shown in the picker.
///     <see cref="DisplayName" /> is refreshed by the owning <see cref="ThemeViewModel" />
///     when the active locale changes.
/// </summary>
public sealed partial class ThemeOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    ///     Gets the resource key used to resolve <see cref="DisplayName" /> from
    ///     the active locale.
    /// </summary>
    public string ResourceKey { get; }

    /// <summary>
    ///     Gets the theme enum value applied when this option is selected.
    /// </summary>
    public AppTheme Theme { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThemeOptionViewModel" />.
    /// </summary>
    /// <param name="resourceKey">The resource key that resolves <see cref="DisplayName" />.</param>
    /// <param name="displayName">The initial localized display name.</param>
    /// <param name="theme">The theme variant this option represents.</param>
    public ThemeOptionViewModel(string resourceKey, string displayName, AppTheme theme)
    {
        ResourceKey = resourceKey;
        _displayName = displayName;
        Theme = theme;
    }
}
