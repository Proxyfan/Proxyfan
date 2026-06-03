using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleProfilePresetViewModel" />.
/// </summary>
public sealed class ThrottleProfilePresetViewModelTests
{
    [Test]
    public async Task Constructor_NullProfile_RepresentsOff()
    {
        var viewModel = new ThrottleProfilePresetViewModel("Off", "Off", null);

        await Assert.That(viewModel.PresetId).IsEqualTo("Off");
        await Assert.That(viewModel.DisplayName).IsEqualTo("Off");
        await Assert.That(viewModel.Profile).IsNull();
    }

    [Test]
    public async Task Constructor_WithProfile_StoresReference()
    {
        var profile = ThrottleProfilePresets.Wireless();

        var viewModel = new ThrottleProfilePresetViewModel("WiFi", "WiFi", profile);

        await Assert.That(viewModel.PresetId).IsEqualTo("WiFi");
        await Assert.That(viewModel.DisplayName).IsEqualTo("WiFi");
        await Assert.That(viewModel.Profile).IsSameReferenceAs(profile);
    }

    [Test]
    public async Task DisplayName_Setter_RaisesPropertyChanged()
    {
        var viewModel = new ThrottleProfilePresetViewModel("WiFi", "WiFi", ThrottleProfilePresets.Wireless());
        var raised = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ThrottleProfilePresetViewModel.DisplayName))
            {
                raised++;
            }
        };

        viewModel.DisplayName = "Sans fil";

        await Assert.That(viewModel.DisplayName).IsEqualTo("Sans fil");
        await Assert.That(raised).IsEqualTo(1);
    }
}
