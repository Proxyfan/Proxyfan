using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Localization;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleViewModel" />.
/// </summary>
public sealed class ThrottleViewModelTests
{
    private static readonly string[] ExpectedPresetIds = ["Off", "2G", "3G", "4G", "WiFi", "Bad Network", "100% Loss"];
    private static readonly string[] ExpectedDisplayNames = ["Off", "2G", "3G", "4G", "WiFi", "Bad Network", "100% Loss"];

    /// <summary>
    ///     Verifies that the view model exposes the seven built-in presets in fixed order
    ///     with stable identifiers.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultHolder_ExposesSevenPresetsWithStableIds()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());

        var ids = viewModel.Presets.Select(p => p.PresetId).ToArray();

        await Assert.That(ids).IsEquivalentTo(ExpectedPresetIds);
    }

    /// <summary>
    ///     Verifies that the view model resolves preset display names through the
    ///     <see cref="LocalizationService" /> rather than embedding English literals.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultHolder_ResolvesLocalizedDisplayNames()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());

        var names = viewModel.Presets.Select(p => p.DisplayName).ToArray();

        await Assert.That(names).IsEquivalentTo(ExpectedDisplayNames);
    }

    /// <summary>
    ///     Verifies that the view model initially selects the "Off" preset when no profile is active.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultHolder_SelectsOffPreset()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());

        await Assert.That(viewModel.SelectedPreset!.PresetId).IsEqualTo("Off");
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");
    }

    /// <summary>
    ///     Verifies that when seeded with an active profile, the matching preset is selected.
    /// </summary>
    [Test]
    public async Task Constructor_SeededHolderWithKnownProfile_SelectsMatchingPreset()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());

        await Assert.That(viewModel.SelectedPreset!.PresetId).IsEqualTo("3G");
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("3G");
    }

    /// <summary>
    ///     Verifies that ApplyCommand with the "Off" preset disables throttling.
    /// </summary>
    [Test]
    public async Task ApplyCommand_OffPreset_DisablesThrottle()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());
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
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());
        var badNetwork = viewModel.Presets.First(p => p.PresetId == "Bad Network");
        viewModel.SelectedPreset = badNetwork;

        viewModel.ApplyCommand.Execute(null);

        await Assert.That(holder.Profile).IsNotNull();
        await Assert.That(holder.Profile!.Name).IsEqualTo(badNetwork.Profile!.Name);
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo(badNetwork.DisplayName);
    }

    /// <summary>
    ///     Verifies that when the selection is null, ApplyCommand is a no-op.
    /// </summary>
    [Test]
    public async Task ApplyCommand_NullSelection_DoesNothing()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.Wireless());
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());
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
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());

        holder.SetProfile(ThrottleProfilePresets.FastFourthGeneration());

        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("4G");
        await Assert.That(viewModel.SelectedPreset!.PresetId).IsEqualTo("4G");
    }

    /// <summary>
    ///     Verifies that disposing the view model unsubscribes from the holder.
    /// </summary>
    [Test]
    public async Task Dispose_AfterChange_StopsReceivingUpdates()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());
        viewModel.Dispose();

        holder.SetProfile(ThrottleProfilePresets.SlowSecondGeneration());

        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");
    }

    /// <summary>
    ///     Verifies that mutating the holder to an unknown profile name results in no matching preset
    ///     and that the active label falls back to the raw profile name.
    /// </summary>
    [Test]
    public async Task ExternalChange_UnknownProfile_LeavesSelectedPresetNull()
    {
        var holder = new MutableThrottleProfile();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, CreateLocalization());
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

    /// <summary>
    ///     Verifies that the view model survives a UI culture switch on the shared
    ///     <see cref="LocalizationService" /> and that stable identifiers and active
    ///     label remain coherent after the refresh.
    /// </summary>
    [Test]
    public async Task CurrentCulture_ChangesToFrFr_RefreshesPresetDisplayNames()
    {
        var holder = new MutableThrottleProfile(ThrottleProfilePresets.ThirdGeneration());
        var localization = CreateLocalization();
        var viewModel = new ThrottleViewModel(holder, InlineUserInterfaceScheduler.Instance, localization);
        var offPreset = viewModel.Presets[0];

        localization.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        // Stable identifiers are unaffected by culture changes.
        await Assert.That(offPreset.PresetId).IsEqualTo("Off");
        // With no fr-FR satellite registered the resource falls back to the
        // invariant value, so the display label and active label remain
        // resolvable (and equal to the invariant value) rather than throwing.
        await Assert.That(offPreset.DisplayName).IsEqualTo("Off");
        await Assert.That(viewModel.ActiveProfileName).IsEqualTo("3G");
    }

    private static LocalizationService CreateLocalization()
    {
        var localization = new LocalizationService(CultureInfo.InvariantCulture);
        var clientAssembly = typeof(Proxyfan.Client.App).Assembly;
        var manager = new ResourceManager("Proxyfan.Client.Resources.Strings", clientAssembly);
        localization.RegisterManager(manager);
        return localization;
    }
}
