using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Theming;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThemeOptionViewModel" />.
/// </summary>
public sealed class ThemeOptionViewModelTests
{
    [Test]
    public async Task Constructor_FromValues_StoresProperties()
    {
        var viewModel = new ThemeOptionViewModel("Tools_Theme_Option_Light", "Light Theme", AppTheme.Light);

        await Assert.That(viewModel.ResourceKey).IsEqualTo("Tools_Theme_Option_Light");
        await Assert.That(viewModel.DisplayName).IsEqualTo("Light Theme");
        await Assert.That(viewModel.Theme).IsEqualTo(AppTheme.Light);
    }

    [Test]
    public async Task Constructor_FromDarkTheme_StoresDarkValue()
    {
        var viewModel = new ThemeOptionViewModel("Tools_Theme_Option_Dark", "Dark Theme", AppTheme.Dark);

        await Assert.That(viewModel.Theme).IsEqualTo(AppTheme.Dark);
    }

    [Test]
    public async Task DisplayName_WhenSet_RaisesPropertyChanged()
    {
        var viewModel = new ThemeOptionViewModel("Tools_Theme_Option_Light", "Light", AppTheme.Light);
        var raised = false;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ThemeOptionViewModel.DisplayName))
            {
                raised = true;
            }
        };

        viewModel.DisplayName = "Clair";

        await Assert.That(raised).IsTrue();
    }
}
