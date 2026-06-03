using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Theming;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the Theme picker tool window
///     (<c>docs/DESIGN.md § 8 Theming and Appearance</c>). Three options
///     (System, Light, Dark) are exposed; applying an option propagates to the
///     shared <see cref="ThemeService" /> and an external theme change is
///     mirrored back to the selected option.
/// </summary>
public sealed class ThemeViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Options_FreshViewModel_ContainsSystemLightDarkInOrder()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var themeService = new ThemeService(AppTheme.System);
            using var vm = new ThemeViewModel(themeService, new LocalizationService(CultureInfo.InvariantCulture));

            await Assert.That(vm.Options.Count).IsEqualTo(3);
            await Assert.That(vm.Options[0].Theme).IsEqualTo(AppTheme.System);
            await Assert.That(vm.Options[1].Theme).IsEqualTo(AppTheme.Light);
            await Assert.That(vm.Options[2].Theme).IsEqualTo(AppTheme.Dark);
        });
    }

    [Test]
    public async Task SelectedOption_FreshViewModelWithSystem_MatchesCurrentTheme()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var themeService = new ThemeService(AppTheme.Light);
            using var vm = new ThemeViewModel(themeService, new LocalizationService(CultureInfo.InvariantCulture));

            await Assert.That(vm.SelectedOption).IsNotNull();
            await Assert.That(vm.SelectedOption!.Theme).IsEqualTo(AppTheme.Light);
        });
    }

    [Test]
    public async Task ApplyCommand_WithDarkSelected_SwitchesThemeService()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var themeService = new ThemeService(AppTheme.System);
            using var vm = new ThemeViewModel(themeService, new LocalizationService(CultureInfo.InvariantCulture));
            vm.SelectedOption = vm.Options.First(o => o.Theme == AppTheme.Dark);

            vm.ApplyCommand.Execute(null);

            await Assert.That(themeService.CurrentTheme).IsEqualTo(AppTheme.Dark);
        });
    }

    [Test]
    public async Task ApplyCommand_NoSelection_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var themeService = new ThemeService(AppTheme.Light);
            using var vm = new ThemeViewModel(themeService, new LocalizationService(CultureInfo.InvariantCulture));
            vm.SelectedOption = null;

            vm.ApplyCommand.Execute(null);

            await Assert.That(themeService.CurrentTheme).IsEqualTo(AppTheme.Light);
        });
    }

    [Test]
    public async Task SelectedOption_ExternalThemeChange_MirrorsBack()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var themeService = new ThemeService(AppTheme.System);
            using var vm = new ThemeViewModel(themeService, new LocalizationService(CultureInfo.InvariantCulture));

            themeService.SwitchTheme(AppTheme.Dark);

            await Assert.That(vm.SelectedOption).IsNotNull();
            await Assert.That(vm.SelectedOption!.Theme).IsEqualTo(AppTheme.Dark);
        });
    }
}
