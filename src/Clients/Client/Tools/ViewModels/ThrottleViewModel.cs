using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Throttle tool window. Binds to a <see cref="MutableThrottleProfile" />
///     and lets the user pick one of the built-in presets (2G, 3G, 4G, WiFi, Bad Network,
///     100% Loss) or disable throttling entirely. Preset display names are sourced from
///     the client Resources table so they follow the active UI culture; matching against
///     externally-supplied profiles uses the stable English
///     <see cref="ThrottleProfilePresetViewModel.PresetId" />.
/// </summary>
public sealed partial class ThrottleViewModel : ObservableObject, IDisposable
{
    private readonly LocalizationService _localization;
    private readonly MutableThrottleProfile _mutableProfile;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _activeProfileName;
    [ObservableProperty]
    private ThrottleProfilePresetViewModel? _selectedPreset;

    /// <summary>
    ///     Gets the list of available throttle profile presets.
    /// </summary>
    public ObservableCollection<ThrottleProfilePresetViewModel> Presets { get; }

    /// <summary>
    ///     Initializes a new <see cref="ThrottleViewModel" /> bound to the mutable profile holder.
    /// </summary>
    /// <param name="mutableProfile">The mutable runtime holder for the active throttle profile.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    /// <param name="localization">The localization service used to resolve preset display names.</param>
    public ThrottleViewModel(MutableThrottleProfile mutableProfile, IUserInterfaceScheduler userInterfaceScheduler, LocalizationService localization)
    {
        _mutableProfile = mutableProfile;
        _userInterfaceScheduler = userInterfaceScheduler;
        _localization = localization;
        var off = CreatePreset(ThrottlePresetLabels.OffPresetId, null);
        var secondGen = CreatePreset("2G", ThrottleProfilePresets.SlowSecondGeneration());
        var thirdGen = CreatePreset("3G", ThrottleProfilePresets.ThirdGeneration());
        var fourthGen = CreatePreset("4G", ThrottleProfilePresets.FastFourthGeneration());
        var wireless = CreatePreset("WiFi", ThrottleProfilePresets.Wireless());
        var badNetwork = CreatePreset("Bad Network", ThrottleProfilePresets.BadNetwork());
        var completeLoss = CreatePreset("100% Loss", ThrottleProfilePresets.CompleteLoss());
        Presets = [off, secondGen, thirdGen, fourthGen, wireless, badNetwork, completeLoss];
        _activeProfileName = ResolveActiveLabel(mutableProfile.Profile);
        _selectedPreset = FindMatchingPreset(mutableProfile.Profile);
        _mutableProfile.Changed += OnProfileChanged;
        _localization.PropertyChanged += OnLocalizationChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutableProfile.Changed -= OnProfileChanged;
        _localization.PropertyChanged -= OnLocalizationChanged;
    }

    [RelayCommand]
    private void Apply()
    {
        var preset = SelectedPreset;
        if (preset is null)
        {
            return;
        }

        if (preset.Profile is null)
        {
            _mutableProfile.Disable();
        }
        else
        {
            _mutableProfile.SetProfile(preset.Profile);
        }
    }

    private ThrottleProfilePresetViewModel CreatePreset(string presetId, ThrottleProfile? profile)
    {
        var displayName = ThrottlePresetLabels.GetLabel(presetId, _localization);
        return new ThrottleProfilePresetViewModel(presetId, displayName, profile);
    }

    private ThrottleProfilePresetViewModel? FindMatchingPreset(ThrottleProfile? profile)
    {
        var targetPresetId = profile?.Name ?? ThrottlePresetLabels.OffPresetId;
        foreach (var preset in Presets)
        {
            if (string.Equals(preset.PresetId, targetPresetId, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(LocalizationService.CurrentCulture))
        {
            return;
        }

        foreach (var preset in Presets)
        {
            preset.DisplayName = ThrottlePresetLabels.GetLabel(preset.PresetId, _localization);
        }

        ActiveProfileName = ResolveActiveLabel(_mutableProfile.Profile);
    }

    private void OnProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() =>
        {
            ActiveProfileName = ResolveActiveLabel(profile);
            SelectedPreset = FindMatchingPreset(profile);
        });
    }

    private string ResolveActiveLabel(ThrottleProfile? profile)
    {
        var presetId = profile?.Name ?? ThrottlePresetLabels.OffPresetId;
        return ThrottlePresetLabels.GetLabel(presetId, _localization);
    }
}
