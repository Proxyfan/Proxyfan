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

    private static LocalizationService CreateLocalization()
    {
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        localization.RegisterManager(new ResourceManager(
            "Proxyfan.Client.Resources.Strings",
            typeof(Proxyfan.Client.App).Assembly));
        return localization;
    }

    /// <summary>
    ///     Verifies that the view model exposes the three theme options in System/Light/Dark order
    ///     with display names resolved from the localization service.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultService_ExposesThreeOptions()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalization());

        var names = viewModel.Options.Select(o => o.DisplayName).ToArray();

        await Assert.That(names).IsEquivalentTo(ExpectedOptionNames);
    }

    /// <summary>
    ///     Verifies that the initially selected option matches the current theme.
    /// </summary>
    [Test]
    public async Task Constructor_SeededWithDark_SelectsDark()
    {
        var service = new ThemeService(AppTheme.Dark);
        var viewModel = new ThemeViewModel(service, CreateLocalization());

        await Assert.That(viewModel.SelectedOption!.Theme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that ApplyCommand pushes the selected option to the theme service.
    /// </summary>
    [Test]
    public async Task ApplyCommand_LightOption_SwitchesServiceToLight()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service, CreateLocalization());
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
        var viewModel = new ThemeViewModel(service, CreateLocalization());
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
        var viewModel = new ThemeViewModel(service, CreateLocalization());

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
        var viewModel = new ThemeViewModel(service, CreateLocalization());
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
        var viewModel = new ThemeViewModel(service, CreateLocalization());

        service.SwitchTheme((AppTheme)999);

        await Assert.That(viewModel.SelectedOption).IsNull();
    }

    /// <summary>
    ///     Verifies that changing the active culture refreshes the option display names so
    ///     the picker reflects runtime language switching.
    /// </summary>
    [Test]
    public async Task LocaleChange_AfterConstruction_RaisesDisplayNameUpdateOnOptions()
    {
        var service = new ThemeService(AppTheme.System);
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        localization.RegisterManager(new MultiCultureResourceManager());
        var viewModel = new ThemeViewModel(service, localization);
        var raised = false;
        viewModel.Options[0].PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ThemeOptionViewModel.DisplayName))
            {
                raised = true;
            }
        };

        localization.CurrentCulture = new CultureInfo("fr-FR");

        await Assert.That(raised).IsTrue();
        await Assert.That(viewModel.Options[0].DisplayName).IsEqualTo("Système");
    }

    /// <summary>
    ///     Verifies that disposing the view model also disposes the option subscriptions
    ///     so they stop receiving locale updates.
    /// </summary>
    [Test]
    public async Task Dispose_AfterLocaleChange_StopsRefreshingOptionDisplayNames()
    {
        var service = new ThemeService(AppTheme.System);
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        localization.RegisterManager(new MultiCultureResourceManager());
        var viewModel = new ThemeViewModel(service, localization);
        viewModel.Dispose();
        var raised = false;
        viewModel.Options[0].PropertyChanged += (_, _) => raised = true;

        localization.CurrentCulture = new CultureInfo("fr-FR");

        await Assert.That(raised).IsFalse();
    }
}

/// <summary>
///     Test resource manager that returns different strings for invariant vs. fr-FR
///     so locale-change behavior can be observed without shipping translations.
/// </summary>
internal sealed class MultiCultureResourceManager : ResourceManager
{
    public override string? GetString(string name, CultureInfo? culture)
    {
        var isFrench = culture is not null && culture.Name.StartsWith("fr", System.StringComparison.OrdinalIgnoreCase);
        return name switch
        {
            "Tools_Theme_Option_System" => isFrench ? "Système" : "System",
            "Tools_Theme_Option_Light" => isFrench ? "Clair" : "Light",
            "Tools_Theme_Option_Dark" => isFrench ? "Sombre" : "Dark",
            _ => null,
        };
    }
}
