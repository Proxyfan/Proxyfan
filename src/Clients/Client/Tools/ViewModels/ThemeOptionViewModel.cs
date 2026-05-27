using Proxyfan.Presentation.Theming;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Lightweight read-only DTO representing one theme option shown in the picker.
/// </summary>
public sealed class ThemeOptionViewModel
{
    /// <summary>
    ///     Gets the user-visible display name of the theme.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the theme enum value applied when this option is selected.
    /// </summary>
    public AppTheme Theme { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThemeOptionViewModel" />.
    /// </summary>
    /// <param name="displayName">The user-visible name.</param>
    /// <param name="theme">The theme variant this option represents.</param>
    public ThemeOptionViewModel(string displayName, AppTheme theme)
    {
        DisplayName = displayName;
        Theme = theme;
    }
}
