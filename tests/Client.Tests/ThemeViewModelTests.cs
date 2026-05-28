using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Theming;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThemeViewModel" />.
/// </summary>
public sealed class ThemeViewModelTests
{
    private static readonly string[] ExpectedOptionNames = ["System", "Light", "Dark"];

    /// <summary>
    ///     Verifies that the view model exposes the three theme options in System/Light/Dark order.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultService_ExposesThreeOptions()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service);

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
        var viewModel = new ThemeViewModel(service);

        await Assert.That(viewModel.SelectedOption!.Theme).IsEqualTo(AppTheme.Dark);
    }

    /// <summary>
    ///     Verifies that ApplyCommand pushes the selected option to the theme service.
    /// </summary>
    [Test]
    public async Task ApplyCommand_LightOption_SwitchesServiceToLight()
    {
        var service = new ThemeService(AppTheme.System);
        var viewModel = new ThemeViewModel(service);
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
        var viewModel = new ThemeViewModel(service);
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
        var viewModel = new ThemeViewModel(service);

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
        var viewModel = new ThemeViewModel(service);
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
        var viewModel = new ThemeViewModel(service);

        service.SwitchTheme((AppTheme)999);

        await Assert.That(viewModel.SelectedOption).IsNull();
    }
}
