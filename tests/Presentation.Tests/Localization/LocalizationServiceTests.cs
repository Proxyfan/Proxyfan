using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Tests.Stubs;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace Proxyfan.Presentation.Tests.Localization;

/// <summary>
///     Tests for <see cref="LocalizationService" />.
/// </summary>
[NotInParallel]
public sealed class LocalizationServiceTests
{
    /// <summary>
    ///     Verifies that the indexer returns a culture-specific value after the locale changes.
    /// </summary>
    [Test]
    public async Task Indexer_AfterLocaleChange_ReturnsUpdatedValue()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var resourceManager = CreateStubResourceManager();
            localizationService.RegisterManager(resourceManager);
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            await Assert.That(localizationService["Greeting"]).IsEqualTo("Bonjour");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that the indexer returns a localized value from a registered resource manager.
    /// </summary>
    [Test]
    public async Task Indexer_RegisteredManager_ReturnsValue()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var resourceManager = CreateStubResourceManager();
            localizationService.RegisterManager(resourceManager);
            await Assert.That(localizationService["Greeting"]).IsEqualTo("Hello");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that the indexer returns the key when no resource manager resolves it.
    /// </summary>
    [Test]
    public async Task Indexer_UnregisteredKey_ReturnsKey()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            await Assert.That(localizationService["MissingKey"]).IsEqualTo("MissingKey");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that changing the current culture raises property change notifications.
    /// </summary>
    [Test]
    public async Task SetCurrentCulture_WhenChanged_FiresPropertyChanged()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            var propertyNames = new List<string>();
            localizationService.PropertyChanged += HandlePropertyChanged;
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            localizationService.PropertyChanged -= HandlePropertyChanged;
            await Assert.That(propertyNames).Contains(nameof(LocalizationService.CurrentCulture));
            await Assert.That(propertyNames).Contains("Item[]");

            void HandlePropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
            {
                if (propertyChangedEventArgs.PropertyName is string propertyName)
                {
                    propertyNames.Add(propertyName);
                }
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that changing the current culture updates <see cref="CultureInfo.CurrentUICulture" />.
    /// </summary>
    [Test]
    public async Task SetCurrentCulture_WhenChanged_UpdatesCurrentUICulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var localizationService = CreateLocalizationService("en-US");
            localizationService.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            await Assert.That(CultureInfo.CurrentUICulture.Name).IsEqualTo("fr-FR");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> CreateLocalizedValues()
    {
        var localizedValues = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string>
            {
                ["Greeting"] = "Hello",
            },
            ["fr-FR"] = new Dictionary<string, string>
            {
                ["Greeting"] = "Bonjour",
            },
        };
        return localizedValues;
    }

    private static LocalizationService CreateLocalizationService(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var localizationService = new LocalizationService(culture);
        return localizationService;
    }

    private static StubResourceManager CreateStubResourceManager()
    {
        var localizedValues = CreateLocalizedValues();
        var resourceManager = new StubResourceManager(localizedValues);
        return resourceManager;
    }
}