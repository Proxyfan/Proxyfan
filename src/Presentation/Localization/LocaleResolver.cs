using System.Globalization;

namespace Proxyfan.Presentation.Localization;

/// <summary>
///     Resolves the UI culture used by the presentation layer.
/// </summary>
public static class LocaleResolver
{
    /// <summary>
    ///     Resolves the configured locale or falls back to the current UI culture.
    /// </summary>
    /// <param name="configuredLocale">The locale configured by the user, if any.</param>
    /// <returns>The resolved UI culture.</returns>
    public static CultureInfo Resolve(string? configuredLocale)
    {
        if (string.IsNullOrWhiteSpace(configuredLocale))
        {
            return CultureInfo.CurrentUICulture;
        }

        try
        {
            var normalizedLocale = configuredLocale.Trim();
            return CultureInfo.GetCultureInfo(normalizedLocale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }
}