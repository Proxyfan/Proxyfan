using System;

namespace Proxyfan.Presentation.Theming;

/// <summary>
///     Parses the persisted string representation of an <see cref="AppTheme" /> back into the
///     enum value. Centralized here so the Preferences UI and other consumers all share the
///     same mapping (case-insensitive; unknown values fall back to <see cref="AppTheme.System" />).
/// </summary>
public static class AppThemeParser
{
    /// <summary>
    ///     Parses <paramref name="value" /> into an <see cref="AppTheme" />. Unknown or null
    ///     inputs are mapped to <see cref="AppTheme.System" />.
    /// </summary>
    /// <param name="value">The textual representation of the theme.</param>
    /// <returns>The parsed theme.</returns>
    public static AppTheme Parse(string? value)
    {
        if (string.Equals(value, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return AppTheme.Light;
        }

        if (string.Equals(value, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return AppTheme.Dark;
        }

        return AppTheme.System;
    }
}
