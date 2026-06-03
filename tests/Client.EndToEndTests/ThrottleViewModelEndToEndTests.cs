using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the Network Throttling tool window
///     (<c>docs/DESIGN.md § 6.12 Network Throttling</c>). Seven presets are
///     exposed (Off, 2G, 3G, 4G, WiFi, Bad Network, 100% Loss); applying one
///     sets it on the shared <see cref="MutableThrottleProfile" /> via the
///     <see cref="IThrottleProfileCoordinator" /> boundary, and an external
///     change mirrors back into the view-model.
/// </summary>
public sealed class ThrottleViewModelEndToEndTests : EndToEndTestBase
{
    private static readonly string[] ExpectedPresetOrder =
        ["Off", "2G", "3G", "4G", "WiFi", "Bad Network", "100% Loss"];

    private static ThrottleProfileCoordinator CreateCoordinator(MutableThrottleProfile holder)
        => new(holder, InlineUserInterfaceScheduler.Instance);

    [Test]
    public async Task Presets_FreshViewModel_ExposesAllSevenInExpectedOrder()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);

            var names = vm.Presets.Select(p => p.DisplayName).ToArray();

            await Assert.That(names).IsEquivalentTo(ExpectedPresetOrder);
        });
    }

    [Test]
    public async Task SelectedPreset_FreshViewModelWithDisabledProfile_IsOff()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);

            await Assert.That(vm.SelectedPreset).IsNotNull();
            await Assert.That(vm.SelectedPreset!.DisplayName).IsEqualTo("Off");
        });
    }

    [Test]
    public async Task ActiveProfileName_FreshViewModel_IsOff()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);

            await Assert.That(vm.ActiveProfileName).IsEqualTo("Off");
        });
    }

    [Test]
    public async Task ApplyCommand_PresetSelected_PropagatesToProfile()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);
            vm.SelectedPreset = vm.Presets.First(p => p.DisplayName == "3G");

            vm.ApplyCommand.Execute(null);

            await Assert.That(profile.Profile).IsNotNull();
            await Assert.That(profile.Profile!.Name).IsEqualTo("3G");
        });
    }

    [Test]
    public async Task ApplyCommand_OffPresetSelected_DisablesProfile()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);
            vm.SelectedPreset = vm.Presets.First(p => p.DisplayName == "Off");

            vm.ApplyCommand.Execute(null);

            await Assert.That(profile.Profile).IsNull();
        });
    }

    [Test]
    public async Task ApplyCommand_NoSelection_IsNoOp()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);
            vm.SelectedPreset = null;

            vm.ApplyCommand.Execute(null);

            await Assert.That(profile.Profile).IsNull();
        });
    }

    [Test]
    public async Task ActiveProfileName_ExternalProfileChange_MirrorsBack()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile();
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);

            profile.SetProfile(ThrottleProfilePresets.FastFourthGeneration());

            await Assert.That(vm.ActiveProfileName).IsEqualTo("4G");
            await Assert.That(vm.SelectedPreset!.DisplayName).IsEqualTo("4G");
        });
    }

    [Test]
    public async Task ActiveProfileName_ExternalDisable_FlipsToOff()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var profile = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
            using var vm = new ThrottleViewModel(CreateCoordinator(profile), InlineUserInterfaceScheduler.Instance, localizationService: null);

            profile.Disable();

            await Assert.That(vm.ActiveProfileName).IsEqualTo("Off");
            await Assert.That(vm.SelectedPreset!.DisplayName).IsEqualTo("Off");
        });
    }
}
