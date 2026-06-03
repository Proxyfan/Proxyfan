using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the DNS Spoofing tool window. Manages the live
///     <see cref="DomainNameSystemOverrideMap" /> through an observable collection of
///     entries and provides commands for adding, removing, enabling and disabling
///     overrides, plus a master active toggle. Match counts are surfaced on demand
///     via <see cref="RefreshMatchCountsCommand" />.
/// </summary>
public sealed partial class DomainNameSystemSpoofingViewModel : ObservableObject
{
    private readonly DomainNameSystemOverrideMap _map;
    [ObservableProperty]
    private bool _isActive;
    [ObservableProperty]
    private string _newHostname;
    [ObservableProperty]
    private string _newOverrideAddress;
    [ObservableProperty]
    private string? _validationMessage;

    /// <summary>
    ///     Gets the observable collection of current DNS overrides.
    /// </summary>
    public ObservableCollection<DomainNameSystemSpoofingEntryViewModel> Entries { get; }

    /// <summary>
    ///     Gets a status string suitable for display in the tool window header
    ///     (e.g. <c>Active — spoofing 3 of 5 domains</c> or <c>Inactive</c>).
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (!IsActive)
            {
                return "Inactive";
            }

            var enabled = 0;
            for (var index = 0; index < Entries.Count; index += 1)
            {
                if (Entries[index].IsEnabled)
                {
                    enabled += 1;
                }
            }

            return string.Create(
                CultureInfo.InvariantCulture,
                $"Active — spoofing {enabled} of {Entries.Count} domains");
        }
    }

    /// <summary>
    ///     Initializes a new <see cref="DomainNameSystemSpoofingViewModel" /> bound to
    ///     the supplied override map. Pre-existing entries on the map are surfaced
    ///     immediately.
    /// </summary>
    /// <param name="map">The domain DNS override map to read from and mutate.</param>
    public DomainNameSystemSpoofingViewModel(DomainNameSystemOverrideMap map)
    {
        _map = map;
        _newHostname = string.Empty;
        _newOverrideAddress = string.Empty;
        _validationMessage = null;
        _isActive = map.IsActive;
        Entries = [];
        foreach (var entry in map.GetSnapshot())
        {
            var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, OnEntryIsEnabledChanged);
            viewModel.PropertyChanged += OnEntryPropertyChanged;
            Entries.Add(viewModel);
        }
    }

    [RelayCommand]
    private void AddEntry()
    {
        var rawHostname = NewHostname;
        var addressText = NewOverrideAddress.Trim();
        if (!DomainPatternValidator.HasValidPattern(rawHostname))
        {
            ValidationMessage = "Hostname must be a valid domain name (e.g. api.example.com) or a wildcard pattern (e.g. *.example.com).";
            return;
        }

        if (!IPAddress.TryParse(addressText, out var address))
        {
            ValidationMessage = "Override address must be a valid IPv4 or IPv6 address.";
            return;
        }

        if (_map.HasOverride(rawHostname))
        {
            ValidationMessage = $"Override for '{rawHostname.Trim()}' already exists.";
            return;
        }

        var entry = new DomainNameSystemOverrideEntry(rawHostname, address);
        _map.Add(entry);
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry, OnEntryIsEnabledChanged);
        viewModel.PropertyChanged += OnEntryPropertyChanged;
        Entries.Add(viewModel);
        NewHostname = string.Empty;
        NewOverrideAddress = string.Empty;
        ValidationMessage = null;
        OnPropertyChanged(nameof(StatusDisplay));
    }

    [RelayCommand]
    private void DisableAllEntries()
    {
        for (var index = 0; index < Entries.Count; index += 1)
        {
            Entries[index].IsEnabled = false;
        }
    }

    [RelayCommand]
    private void EnableAllEntries()
    {
        for (var index = 0; index < Entries.Count; index += 1)
        {
            Entries[index].IsEnabled = true;
        }
    }

    private void OnEntryIsEnabledChanged(DomainNameSystemSpoofingEntryViewModel row, bool value)
    {
        _map.HasSetEnabled(row.CanonicalPattern, value);
    }

    private void OnEntryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(DomainNameSystemSpoofingEntryViewModel.IsEnabled))
        {
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    partial void OnIsActiveChanged(bool value)
    {
        if (_map.IsActive != value)
        {
            _map.IsActive = value;
        }

        OnPropertyChanged(nameof(StatusDisplay));
    }

    [RelayCommand]
    private void RefreshMatchCounts()
    {
        var snapshot = _map.GetSnapshot();
        var byPattern = new System.Collections.Generic.Dictionary<string, int>(snapshot.Count, System.StringComparer.Ordinal);
        for (var index = 0; index < snapshot.Count; index += 1)
        {
            var entry = snapshot[index];
            byPattern[entry.CanonicalPattern] = entry.MatchCount;
        }

        for (var index = 0; index < Entries.Count; index += 1)
        {
            var row = Entries[index];
            if (byPattern.TryGetValue(row.CanonicalPattern, out var matchCount))
            {
                row.SyncMatchCount(matchCount);
            }
        }
    }

    [RelayCommand]
    private void RemoveEntry(DomainNameSystemSpoofingEntryViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        viewModel.PropertyChanged -= OnEntryPropertyChanged;
        _map.HasRemoved(viewModel.CanonicalPattern);
        Entries.Remove(viewModel);
        ValidationMessage = null;
        OnPropertyChanged(nameof(StatusDisplay));
    }

    [RelayCommand]
    private void ResetMatchCounts()
    {
        _map.ResetAllMatchCounts();
        for (var index = 0; index < Entries.Count; index += 1)
        {
            Entries[index].SyncMatchCount(0);
        }
    }
}
