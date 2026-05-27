using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using System.Collections.ObjectModel;
using System.Net;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the DNS Spoofing tool window. Manages the live
///     <see cref="DomainNameSystemOverrideMap" /> through an observable collection of
///     entries and provides commands for adding and removing overrides.
/// </summary>
public sealed partial class DomainNameSystemSpoofingViewModel : ObservableObject
{
    private readonly DomainNameSystemOverrideMap _map;
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
    ///     Initializes a new <see cref="DomainNameSystemSpoofingViewModel" /> bound to
    ///     the supplied override map.
    /// </summary>
    /// <param name="map">The domain DNS override map to read from and mutate.</param>
    public DomainNameSystemSpoofingViewModel(DomainNameSystemOverrideMap map)
    {
        _map = map;
        _newHostname = string.Empty;
        _newOverrideAddress = string.Empty;
        _validationMessage = null;
        Entries = [];
    }

    [RelayCommand]
    private void AddEntry()
    {
        var hostname = NewHostname.Trim();
        var addressText = NewOverrideAddress.Trim();
        if (hostname.Length == 0)
        {
            ValidationMessage = "Hostname is required.";
            return;
        }

        if (!IPAddress.TryParse(addressText, out var address))
        {
            ValidationMessage = "Override address must be a valid IPv4 or IPv6 address.";
            return;
        }

        if (_map.HasOverride(hostname))
        {
            ValidationMessage = $"Override for '{hostname}' already exists.";
            return;
        }

        var entry = new DomainNameSystemOverrideEntry(hostname, address);
        _map.Add(entry);
        var viewModel = new DomainNameSystemSpoofingEntryViewModel(entry);
        Entries.Add(viewModel);
        NewHostname = string.Empty;
        NewOverrideAddress = string.Empty;
        ValidationMessage = null;
    }

    [RelayCommand]
    private void RemoveEntry(DomainNameSystemSpoofingEntryViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        _map.HasRemoved(viewModel.Hostname);
        Entries.Remove(viewModel);
        ValidationMessage = null;
    }
}
