using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Localization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ThrottleViewModel" />.
/// </summary>
[NotInParallel]
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
        var viewModel = CreateViewModel(holder, localizationService: null);

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
        var viewModel = CreateViewModel(holder, localizationService: null);

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
        var viewModel = CreateViewModel(holder, localizationService: null);

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
        var viewModel = CreateViewModel(holder, localizationService: null);
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
        var viewModel = CreateViewModel(holder, localizationService: null);
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
        var viewModel = CreateViewModel(holder, localizationService: null);
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
        var viewModel = CreateViewModel(holder, localizationService: null);

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
        var viewModel = CreateViewModel(holder, localizationService: null);
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
        var viewModel = CreateViewModel(holder, localizationService: null);
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
    ///     Verifies that when a localization service is supplied, preset display names
    ///     are resolved from the registered resource manager rather than the stable identifier.
    /// </summary>
    [Test]
    public async Task Constructor_LocalizationServicePresent_ResolvesLocalizedPresetDisplayNames()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var english = CultureInfo.GetCultureInfo("en-US");
            var french = CultureInfo.GetCultureInfo("fr-FR");
            var lookup = new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = new()
                {
                    ["Tools_Throttle_Preset_Off"] = "Off",
                    ["Tools_Throttle_Preset_BadNetwork"] = "Bad Network",
                },
                ["fr-FR"] = new()
                {
                    ["Tools_Throttle_Preset_Off"] = "Désactivé",
                    ["Tools_Throttle_Preset_BadNetwork"] = "Mauvais réseau",
                },
            };
            var localizationService = new LocalizationService(english);
            var stub = new StubResourceManager(lookup);
            localizationService.RegisterManager(stub);
            var holder = new MutableThrottleProfile();
            var viewModel = CreateViewModel(holder, localizationService);
            var offPreset = viewModel.Presets.First(p => p.Identifier == "Off");
            var badNetworkPreset = viewModel.Presets.First(p => p.Identifier == "Bad Network");

            await Assert.That(offPreset.DisplayName).IsEqualTo("Off");
            await Assert.That(badNetworkPreset.DisplayName).IsEqualTo("Bad Network");
            await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Off");

            localizationService.CurrentCulture = french;

            await Assert.That(offPreset.DisplayName).IsEqualTo("Désactivé");
            await Assert.That(badNetworkPreset.DisplayName).IsEqualTo("Mauvais réseau");
            await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Désactivé");
            await Assert.That(offPreset.Identifier).IsEqualTo("Off");
            await Assert.That(badNetworkPreset.Identifier).IsEqualTo("Bad Network");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    ///     Verifies that matching the active profile uses the stable identifier, not
    ///     the (possibly localized) display name.
    /// </summary>
    [Test]
    public async Task ExternalChange_LocalizedCulture_MatchesPresetByStableIdentifier()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var french = CultureInfo.GetCultureInfo("fr-FR");
            var lookup = new Dictionary<string, Dictionary<string, string>>
            {
                ["fr-FR"] = new()
                {
                    ["Tools_Throttle_Preset_3G"] = "Troisième génération",
                },
            };
            var localizationService = new LocalizationService(french);
            localizationService.RegisterManager(new StubResourceManager(lookup));
            var holder = new MutableThrottleProfile();
            var viewModel = CreateViewModel(holder, localizationService);

            holder.SetProfile(ThrottleProfilePresets.ThirdGeneration());

            await Assert.That(viewModel.SelectedPreset).IsNotNull();
            await Assert.That(viewModel.SelectedPreset!.Identifier).IsEqualTo("3G");
            await Assert.That(viewModel.SelectedPreset!.DisplayName).IsEqualTo("Troisième génération");
            await Assert.That(viewModel.ActiveProfileName).IsEqualTo("Troisième génération");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private static ThrottleViewModel CreateViewModel(MutableThrottleProfile holder, LocalizationService? localizationService)
    {
        var coordinator = new ThrottleProfileCoordinator(holder, InlineUserInterfaceScheduler.Instance);
        return new ThrottleViewModel(coordinator, InlineUserInterfaceScheduler.Instance, localizationService);
    }

    private sealed class StubResourceManager : ResourceManager
    {
        private readonly Dictionary<string, Dictionary<string, string>> _entriesByCulture;

        public StubResourceManager(Dictionary<string, Dictionary<string, string>> entriesByCulture)
        {
            _entriesByCulture = entriesByCulture;
        }

        public override string? GetString(string name, CultureInfo? culture)
        {
            var cultureName = culture?.Name ?? string.Empty;
            if (_entriesByCulture.TryGetValue(cultureName, out var entries) && entries.TryGetValue(name, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
