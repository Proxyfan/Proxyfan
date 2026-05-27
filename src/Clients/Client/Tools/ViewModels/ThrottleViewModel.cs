using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Throttle tool window. Binds to a <see cref="MutableThrottleProfile" />
///     and lets the user pick one of the built-in presets (2G, 3G, 4G, WiFi, Bad Network,
///     100% Loss) or disable throttling entirely.
/// </summary>
public sealed partial class ThrottleViewModel : ObservableObject, IDisposable
{
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
    public ThrottleViewModel(MutableThrottleProfile mutableProfile, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _mutableProfile = mutableProfile;
        _userInterfaceScheduler = userInterfaceScheduler;
        var off = new ThrottleProfilePresetViewModel("Off", null);
        var secondGen = new ThrottleProfilePresetViewModel("2G", ThrottleProfilePresets.SlowSecondGeneration());
        var thirdGen = new ThrottleProfilePresetViewModel("3G", ThrottleProfilePresets.ThirdGeneration());
        var fourthGen = new ThrottleProfilePresetViewModel("4G", ThrottleProfilePresets.FastFourthGeneration());
        var wireless = new ThrottleProfilePresetViewModel("WiFi", ThrottleProfilePresets.Wireless());
        var badNetwork = new ThrottleProfilePresetViewModel("Bad Network", ThrottleProfilePresets.BadNetwork());
        var completeLoss = new ThrottleProfilePresetViewModel("100% Loss", ThrottleProfilePresets.CompleteLoss());
        Presets = [off, secondGen, thirdGen, fourthGen, wireless, badNetwork, completeLoss];
        _activeProfileName = mutableProfile.Profile?.Name ?? "Off";
        _selectedPreset = FindMatchingPreset(mutableProfile.Profile);
        _mutableProfile.Changed += OnProfileChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutableProfile.Changed -= OnProfileChanged;
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

    private ThrottleProfilePresetViewModel? FindMatchingPreset(ThrottleProfile? profile)
    {
        var targetName = profile?.Name ?? "Off";
        foreach (var preset in Presets)
        {
            if (string.Equals(preset.DisplayName, targetName, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    private void OnProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() =>
        {
            ActiveProfileName = profile?.Name ?? "Off";
            SelectedPreset = FindMatchingPreset(profile);
        });
    }
}
