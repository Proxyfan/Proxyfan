using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Throttle tool window. Lets the user pick one of the built-in
///     presets (2G, 3G, 4G, WiFi, Bad Network, 100% Loss) or disable throttling entirely.
///     Preset display names are resolved from the <see cref="LocalizationService" /> so they
///     follow the active UI culture, while stable identifiers are used to match the active
///     runtime profile through <see cref="IThrottleCoordinator" />.
/// </summary>
public sealed partial class ThrottleViewModel : ObservableObject, IDisposable
{
    private readonly LocalizationService? _localizationService;
    private readonly Dictionary<ThrottleProfilePresetViewModel, string> _presetResourceKeys;
    private readonly IThrottleCoordinator _throttleCoordinator;
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
    ///     Initializes a new <see cref="ThrottleViewModel" />.
    /// </summary>
    /// <param name="throttleCoordinator">Presentation-facing throttle coordinator.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    /// <param name="localizationService">
    ///     Localization service used to resolve preset display names; pass
    ///     <see langword="null" /> to show the stable identifier verbatim.
    /// </param>
    public ThrottleViewModel(
        IThrottleCoordinator throttleCoordinator,
        IUserInterfaceScheduler userInterfaceScheduler,
        LocalizationService? localizationService)
    {
        _throttleCoordinator = throttleCoordinator;
        _userInterfaceScheduler = userInterfaceScheduler;
        _localizationService = localizationService;
        _presetResourceKeys = [];
        Presets = [];
        var definitions = ThrottlePresetDefinitions.Create();
        foreach (var definition in definitions)
        {
            var displayName = ResolveDisplayName(definition.ResourceKey, definition.Identifier);
            var preset = new ThrottleProfilePresetViewModel(definition.Identifier, displayName);
            _presetResourceKeys[preset] = definition.ResourceKey;
            Presets.Add(preset);
        }

        var initialProfileIdentifier = _throttleCoordinator.ActiveProfileIdentifier;
        _selectedPreset = FindMatchingPreset(initialProfileIdentifier);
        _activeProfileName = ResolveActiveProfileName(initialProfileIdentifier);
        _throttleCoordinator.ProfileChanged += OnProfileChanged;
        if (_localizationService is { } subscribeService)
        {
            subscribeService.PropertyChanged += OnLocalizationChanged;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _throttleCoordinator.ProfileChanged -= OnProfileChanged;
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

        _throttleCoordinator.Apply(preset.Identifier);
    }

    private ThrottleProfilePresetViewModel? FindMatchingPreset(string? activeProfileIdentifier)
    {
        var targetIdentifier = activeProfileIdentifier ?? ThrottlePresetDefinitions.OffIdentifier;
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

            ActiveProfileName = ResolveActiveProfileName(_throttleCoordinator.ActiveProfileIdentifier);
        });
    }

    private void OnProfileChanged(string? activeProfileIdentifier)
    {
        _userInterfaceScheduler.Post(() =>
        {
            ActiveProfileName = ResolveActiveProfileName(activeProfileIdentifier);
            SelectedPreset = FindMatchingPreset(activeProfileIdentifier);
        });
    }

    private string ResolveActiveProfileName(string? activeProfileIdentifier)
    {
        var matched = FindMatchingPreset(activeProfileIdentifier);
        if (matched is not null)
        {
            return matched.DisplayName;
        }

        if (activeProfileIdentifier is not null)
        {
            return activeProfileIdentifier;
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
