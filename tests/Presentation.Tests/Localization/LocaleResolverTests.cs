using Proxyfan.Presentation.Localization;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests.Localization;

/// <summary>
///     Tests for <see cref="LocaleResolver" />.
/// </summary>
[NotInParallel]
public sealed class LocaleResolverTests
{
    /// <summary>
    ///     Verifies that an invalid locale falls back to the current UI culture.
    /// </summary>
    [Test]
    public async Task Resolve_InvalidLocale_ReturnsCurrentUICulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            var culture = LocaleResolver.Resolve("invalid-locale");
            await Assert.That(culture.Name).IsEqualTo("de-DE");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that a null locale falls back to the current UI culture.
    /// </summary>
    [Test]
    public async Task Resolve_NullInput_ReturnsCurrentUICulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var culture = LocaleResolver.Resolve(null);
            await Assert.That(culture.Name).IsEqualTo("ja-JP");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that a valid locale returns the configured culture.
    /// </summary>
    [Test]
    public async Task Resolve_ValidLocale_ReturnsThatCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var culture = LocaleResolver.Resolve("fr-FR");
            await Assert.That(culture.Name).IsEqualTo("fr-FR");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}