using Proxyfan.Domain.Throttling;
using System;
using System.Collections.Generic;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     Bridges the mutable runtime throttle holder into a presentation-safe surface
///     based on stable preset identifiers.
/// </summary>
public sealed class ThrottleCoordinator : IThrottleCoordinator, IDisposable
{
    private event ThrottleCoordinatorProfileChanged? _profileChanged;
    private readonly MutableThrottleProfile _mutableProfile;
    private readonly Dictionary<string, ThrottlePresetFactory> _presetFactories;

    /// <summary>
    ///     Initializes a new <see cref="ThrottleCoordinator" />.
    /// </summary>
    /// <param name="mutableProfile">The runtime holder used by the proxy pipeline.</param>
    public ThrottleCoordinator(MutableThrottleProfile mutableProfile)
    {
        _mutableProfile = mutableProfile;
        var presetFactories = new Dictionary<string, ThrottlePresetFactory>(StringComparer.Ordinal);
        var definitions = ThrottlePresetDefinitions.Create();
        foreach (var definition in definitions)
        {
            presetFactories[definition.Identifier] = definition.ProfileFactory;
        }

        _presetFactories = presetFactories;
        _mutableProfile.Changed += OnProfileChanged;
    }

    /// <inheritdoc />
    string? IThrottleCoordinator.ActiveProfileIdentifier => _mutableProfile.Profile?.Name;

    /// <inheritdoc />
    void IThrottleCoordinator.Apply(string presetIdentifier)
    {
        if (string.IsNullOrWhiteSpace(presetIdentifier))
        {
            return;
        }

        if (!_presetFactories.TryGetValue(presetIdentifier, out var factory))
        {
            return;
        }

        var profile = factory();
        if (profile is null)
        {
            _mutableProfile.Disable();
            return;
        }

        _mutableProfile.SetProfile(profile);
    }

    /// <inheritdoc />
    event ThrottleCoordinatorProfileChanged? IThrottleCoordinator.ProfileChanged
    {
        add => _profileChanged += value;
        remove => _profileChanged -= value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutableProfile.Changed -= OnProfileChanged;
    }

    private void OnProfileChanged(MutableThrottleProfile sender, ThrottleProfile? profile)
    {
        _profileChanged?.Invoke(profile?.Name);
    }
}
