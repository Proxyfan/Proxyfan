using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Theming;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThemeViewModel" />.
/// </summary>
public sealed class ThemeViewModelTests
{
    private static readonly string[] ExpectedOptionNames = ["System", "Light", "Dark"];
    private static readonly string[] ExpectedResourceKeys =
    [
        "Tools_Theme_Option_System",
        "Tools_Theme_Option_Light",
        "Tools_Theme_Option_Dark",
    ];
    private static readonly string[] StaleNames = ["stale", "stale", "stale"];

    /// <summary>
    ///     Verifies that the view model exposes the three theme options in System/Light/Dark order
    ///     with display names resolved through the localization service.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultService_ExposesThreeOptions()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());

        var names = viewModel.Options.Select(o => o.DisplayName).ToArray();

        await Assert.That(names).IsEquivalentTo(ExpectedOptionNames);
    }

    /// <summary>
    ///     Verifies that every option exposes the resource key used to resolve its display name.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultService_ExposesResourceKeys()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());

        var keys = viewModel.Options.Select(o => o.ResourceKey).ToArray();

        await Assert.That(keys).IsEquivalentTo(ExpectedResourceKeys);
    }

    /// <summary>
    ///     Verifies that the initially selected option matches the current theme.
    /// </summary>
    [Test]
    public async Task Constructor_SeededWithDark_SelectsDark()
    {
        var service = new ThemeService(AppTheme.Dark);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());

        await Assert.That(viewModel.SelectedOption!.Theme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that ApplyCommand pushes the selected option to the theme service.
    /// </summary>
    [Test]
    public async Task ApplyCommand_LightOption_SwitchesServiceToLight()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());
        var lightOption = viewModel.Options.First(o => o.Theme == AppTheme.Light);
        viewModel.SelectedOption = lightOption;

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(service.CurrentTheme).IsEqualTo(AppTheme.Light);
    }

    /// <summary>
    ///     Verifies that ApplyCommand is a no-op when no option is selected.
    /// </summary>
    [Test]
    public async Task ApplyCommand_NoSelection_DoesNothing()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());
        viewModel.SelectedOption = null;

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(service.CurrentTheme).IsEqualTo(AppTheme.System);
    }

    /// <summary>
    ///     Verifies that external mutations to the theme service propagate into the view model.
    /// </summary>
    [Test]
    public async Task ExternalChange_DarkTheme_UpdatesSelectedOption()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());

        service.SwitchTheme(AppTheme.Dark);

        await Assert.That(viewModel.SelectedOption!.Theme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes from the theme service.
    /// </summary>
    [Test]
    public async Task Dispose_AfterChange_StopsReceivingUpdates()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());
        viewModel.Dispose();

        service.SwitchTheme(AppTheme.Dark);

        await Assert.That(viewModel.SelectedOption!.Theme).IsEqualTo(AppTheme.System);
    }

    /// <summary>
    ///     Verifies that an unrecognized <see cref="AppTheme" /> value clears the selected
    ///     option (exercises the post-loop fall-through return path in
    ///     <c>ThemeViewModel.FindOption</c>).
    /// </summary>
    [Test]
    public async Task ExternalChange_UnknownTheme_ClearsSelectedOption()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalizationService());

        service.SwitchTheme((AppTheme)999);

        await Assert.That(viewModel.SelectedOption).IsNull();
    }

    /// <summary>
    ///     Verifies that changing the active culture re-resolves option display names so the
    ///     picker reflects runtime language switching.
    /// </summary>
    [Test]
    public async Task CultureChange_AfterConstruction_RefreshesOptionDisplayNames()
    {
        var service = new ThemeService(AppTheme.System);
        var localization = CreateLocalizationService();
        var viewModel = new ThemeViewModel(service, localization);
        foreach (var option in viewModel.Options)
        {
            option.DisplayName = "stale";
        }

        localization.CurrentCulture = new CultureInfo("fr-FR");

        var names = viewModel.Options.Select(o => o.DisplayName).ToArray();
        await Assert.That(names).IsEquivalentTo(ExpectedOptionNames);
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes from the localization service so
    ///     subsequent culture changes do not mutate the option labels.
    /// </summary>
    [Test]
    public async Task Dispose_AfterCultureChange_StopsRefreshingDisplayNames()
    {
        var service = new ThemeService(AppTheme.System);
        var localization = CreateLocalizationService();
        var viewModel = new ThemeViewModel(service, localization);
        viewModel.Dispose();
        foreach (var option in viewModel.Options)
        {
            option.DisplayName = "stale";
        }

        localization.CurrentCulture = new CultureInfo("fr-FR");

        var names = viewModel.Options.Select(o => o.DisplayName).ToArray();
        await Assert.That(names).IsEquivalentTo(StaleNames);
    }

    private static LocalizationService CreateLocalizationService()
    {
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        var clientAssembly = typeof(Proxyfan.Client.App).Assembly;
        var manager = new ResourceManager("Proxyfan.Client.Resources.Strings", clientAssembly);
        localization.RegisterManager(manager);
        return localization;
    }
}
