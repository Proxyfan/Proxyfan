using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using System.Linq;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleViewModel" />.
/// </summary>
public sealed class ThrottleViewModelTests
{
    private static readonly string[] ExpectedPresetNames = ["Off", "2G", "3G", "4G", "WiFi", "Bad Network", "100% Loss"];

    /// <summary>
    ///     Verifies that the view model exposes the seven built-in presets in fixed order.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultHolder_ExposesSevenPresets()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);

        var names = viewModel.Presets.Select(p => p.DisplayName).ToArray();

        await Assert.That(names).IsEquivalentTo(ExpectedPresetNames);
    }

    /// <summary>
    ///     Verifies that the view model initially selects the "Off" preset when no profile is active.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultHolder_SelectsOffPreset()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.SelectedPreset!.DisplayName).IsEqualTo("Off");
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");
    }

    /// <summary>
    ///     Verifies that when seeded with an active profile, the matching preset is selected.
    /// </summary>
    [Test]
    public async Task Constructor_SeededHolderWithKnownProfile_SelectsMatchingPreset()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.SelectedPreset!.DisplayName).IsEqualTo("3G");
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("3G");
    }

    /// <summary>
    ///     Verifies that ApplyCommand with the "Off" preset disables throttling.
    /// </summary>
    [Test]
    public async Task ApplyCommand_OffPreset_DisablesThrottle()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedPreset = viewModel.Presets[0];

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(holder.Profile).IsNull();
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");
    }

    /// <summary>
    ///     Verifies that ApplyCommand with a non-Off preset stores the matching profile.
    /// </summary>
    [Test]
    public async Task ApplyCommand_BadNetworkPreset_StoresBadNetworkProfile()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);
        var badNetwork = viewModel.Presets.First(p => p.DisplayName == "Bad Network");
        viewModel.SelectedPreset = badNetwork;

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo(badNetwork.Profile!.Name);
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo(badNetwork.Profile!.Name);
    }

    /// <summary>
    ///     Verifies that when the selection is null, ApplyCommand is a no-op.
    /// </summary>
    [Test]
    public async Task ApplyCommand_NullSelection_DoesNothing()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);
        viewModel.SelectedPreset = null;

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(holder.Profile).IsNotNull();
    }

    /// <summary>
    ///     Verifies that external mutations to the holder update the view model via the scheduler.
    /// </summary>
    [Test]
    public async Task ExternalChange_FourthGeneration_UpdatesActiveProfileName()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);

        holder.SetProfile(ThrottleProfilePresets.FastFourthGeneration());

        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("4G");
        await Assert.That(viewModel.SelectedPreset!.DisplayName).IsEqualTo("4G");
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes from the holder.
    /// </summary>
    [Test]
    public async Task Dispose_AfterChange_StopsReceivingUpdates()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);
        viewModel.Dispose();

        holder.SetProfile(ThrottleProfilePresets.SlowSecondGeneration());

        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");
    }

    /// <summary>
    ///     Verifies that mutating the holder to an unknown profile name results in no matching preset.
    /// </summary>
    [Test]
    public async Task ExternalChange_UnknownProfile_LeavesSelectedPresetNull()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance);
        var parameters = new ThrottleProfileParameters
        {
            UploadBytesPerSecond = 1,
            DownloadBytesPerSecond = 1,
            Latency = System.TimeSpan.Zero,
            PacketLossProbability = 0,
        };
        var custom = new ThrottleProfile("Mystery", parameters);

        holder.SetProfile(custom);

        await Assert.That(viewModel.SelectedPreset).IsNull();
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Mystery");
    }
}
