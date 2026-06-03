using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Throttling;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Throttle tool window. Binds to a <see cref="IThrottleProfileCoordinator" />
///     and lets the user pick one of the built-in presets (2G, 3G, 4G, WiFi, Bad Network,
///     100% Loss) or disable throttling entirely. Preset display names are resolved from
///     the <see cref="LocalizationService" /> so they follow the active UI culture, while
///     the underlying stable identifiers are used to match the active profile.
/// </summary>
public sealed partial class ThrottleViewModel : ObservableObject, IDisposable
{
    private readonly IThrottleProfileCoordinator _coordinator;
    private readonly LocalizationService? _localizationService;
    private readonly Dictionary<ThrottleProfilePresetViewModel, string> _presetResourceKeys;
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
    ///     Initializes a new <see cref="ThrottleViewModel" /> bound to the throttle profile coordinator.
    /// </summary>
    /// <param name="coordinator">Coordinates the active throttle profile between UI and runtime domain state.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    /// <param name="localizationService">
    ///     Localization service used to resolve preset display names; pass
    ///     <see langword="null" /> to show the stable identifier verbatim.
    /// </param>
    public ThrottleViewModel(
        IThrottleProfileCoordinator coordinator,
        IUserInterfaceScheduler userInterfaceScheduler,
        LocalizationService? localizationService)
    {
        _coordinator = coordinator;
        _userInterfaceScheduler = userInterfaceScheduler;
        _localizationService = localizationService;
        _presetResourceKeys = [];
        Presets = [];
        var definitions = ThrottlePresetDefinitions.Create();
        foreach (var definition in definitions)
        {
            var displayName = ResolveDisplayName(definition.ResourceKey, definition.Identifier);
            var profile = definition.ProfileFactory();
            var preset = new ThrottleProfilePresetViewModel(definition.Identifier, displayName, profile);
            _presetResourceKeys[preset] = definition.ResourceKey;
            Presets.Add(preset);
        }

        _selectedPreset = FindMatchingPreset(_coordinator.Profile);
        _activeProfileName = ResolveActiveProfileName(_coordinator.Profile);
        _coordinator.Changed += OnProfileChanged;
        if (_localizationService is { } subscribeService)
        {
            subscribeService.PropertyChanged += OnLocalizationChanged;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _coordinator.Changed -= OnProfileChanged;
        if (_localizationService is { } unsubscribeService)
        {
            unsubscribeService.PropertyChanged -= OnLocalizationChanged;
        }
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
            _coordinator.Apply(null);
        }
        else
        {
            _coordinator.Apply(preset.Profile);
        }
    }

    private ThrottleProfilePresetViewModel? FindMatchingPreset(ThrottleProfile? profile)
    {
        var targetIdentifier = profile?.Name ?? ThrottlePresetDefinitions.OffIdentifier;
        foreach (var preset in Presets)
        {
            if (string.Equals(preset.Identifier, targetIdentifier, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>
    ///     Refreshes preset display names and the active-profile label when the
    ///     <see cref="LocalizationService" /> reports a culture change. Filters out
    ///     unrelated <see cref="INotifyPropertyChanged" /> events to avoid redundant work.
    /// </summary>
    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs args)
    {
        var propertyName = args.PropertyName;
        if (!string.IsNullOrEmpty(propertyName)
            && !string.Equals(propertyName, nameof(LocalizationService.CurrentCulture), StringComparison.Ordinal)
            && !string.Equals(propertyName, "Item[]", StringComparison.Ordinal))
        {
            return;
        }

        _userInterfaceScheduler.Post(() =>
        {
            foreach (var preset in Presets)
            {
                if (_presetResourceKeys.TryGetValue(preset, out var key))
                {
                    preset.DisplayName = ResolveDisplayName(key, preset.Identifier);
                }
            }

            ActiveProfileName = ResolveActiveProfileName(_coordinator.Profile);
        });
    }

    private void OnProfileChanged(ThrottleProfile? profile)
    {
        _userInterfaceScheduler.Post(() =>
        {
            ActiveProfileName = ResolveActiveProfileName(profile);
            SelectedPreset = FindMatchingPreset(profile);
        });
    }

    private string ResolveActiveProfileName(ThrottleProfile? profile)
    {
        var matched = FindMatchingPreset(profile);
        if (matched is not null)
        {
            return matched.DisplayName;
        }

        var profileName = profile?.Name;
        if (profileName is not null)
        {
            return profileName;
        }

        return ResolveDisplayName(ThrottlePresetDefinitions.OffResourceKey, ThrottlePresetDefinitions.OffIdentifier);
    }

    /// <summary>
    ///     Resolves the localized display name for a preset, falling back to
    ///     <paramref name="fallback" /> when no <see cref="LocalizationService" /> is
    ///     available or the resource is missing. <see cref="LocalizationService" />
    ///     returns the resource key itself when a key is not registered; this method
    ///     treats that case as a miss to avoid surfacing raw resource keys in the UI.
    /// </summary>
    /// <param name="resourceKey">The resource key to resolve.</param>
    /// <param name="fallback">The fallback string returned when no localized value exists.</param>
    /// <returns>The resolved localized display name, or <paramref name="fallback" />.</returns>
    private string ResolveDisplayName(string resourceKey, string fallback)
    {
        if (_localizationService is null)
        {
            return fallback;
        }

        var value = _localizationService[resourceKey];
        if (string.IsNullOrEmpty(value) || string.Equals(value, resourceKey, StringComparison.Ordinal))
        {
            return fallback;
        }

        return value;
    }
}
