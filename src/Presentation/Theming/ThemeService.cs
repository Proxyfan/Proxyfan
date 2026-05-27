using System.ComponentModel;

namespace Proxyfan.Presentation.Theming;

/// <summary>
///     Tracks the currently selected theme variant and raises
///     <see cref="INotifyPropertyChanged.PropertyChanged" /> when the selection changes.
///     UI hosts subscribe to the change event to apply the new theme to the application styles.
/// </summary>
public sealed class ThemeService : INotifyPropertyChanged
{
    /// <summary>
    ///     Occurs when <see cref="CurrentTheme" /> or one of its derived values changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Occurs after the active theme has been switched, with the new theme as the argument.
    /// </summary>
    public event ThemeChangedHandler? ThemeChanged;

    private AppTheme _currentTheme;

    /// <summary>
    ///     Gets the currently selected theme variant.
    /// </summary>
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme == value)
            {
                return;
            }

            _currentTheme = value;
            var propertyChangedArgs = new PropertyChangedEventArgs(nameof(CurrentTheme));
            PropertyChanged?.Invoke(this, propertyChangedArgs);
            ThemeChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="ThemeService" /> with the supplied starting theme.
    /// </summary>
    /// <param name="initialTheme">The theme to begin with.</param>
    public ThemeService(AppTheme initialTheme)
    {
        _currentTheme = initialTheme;
    }

    /// <summary>
    ///     Switches the active theme. No-op when <paramref name="theme" /> equals the current
    ///     selection.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    public void SwitchTheme(AppTheme theme)
    {
        CurrentTheme = theme;
    }
}
