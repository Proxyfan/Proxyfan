namespace Proxyfan.Presentation.Theming;

/// <summary>
///     Delegate for theme-changed notifications raised by <see cref="ThemeService" />.
/// </summary>
/// <param name="sender">The service raising the event.</param>
/// <param name="theme">The newly active theme.</param>
public delegate void ThemeChangedHandler(object? sender, AppTheme theme);
