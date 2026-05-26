using Microsoft.Extensions.DependencyInjection;
using Proxyfan.Presentation.Localization;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests.Localization;

/// <summary>
///     Additional tests for <see cref="LocalizationService" /> focused on branch coverage.
/// </summary>
[NotInParallel]
public sealed class LocalizationServiceAdditionalTests
{
    /// <summary>
    ///     Verifies that setting the current culture to the same value as before is a no-op.
    /// </summary>
    [Test]
    public async Task SetCurrentCulture_WhenSameCulture_DoesNotFirePropertyChanged()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            var service = new LocalizationService(culture);
            var fired = false;
            service.PropertyChanged += (_, _) => fired = true;

            service.CurrentCulture = culture;

            await Assert.That(fired).IsFalse();
            await Assert.That(service.CurrentCulture.Name).IsEqualTo("en-US");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}