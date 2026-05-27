using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Map Remote tool window. Binds to <see cref="MutableMapRemoteRule" />
///     and exposes the current entries as an observable collection plus an editor for
///     adding new entries.
/// </summary>
public sealed partial class MapRemoteViewModel : ObservableObject, IDisposable
{
    private readonly MutableMapRemoteRule _rule;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _destinationHost;
    [ObservableProperty]
    private string _destinationPath;
    [ObservableProperty]
    private string _destinationPort;
    [ObservableProperty]
    private string _destinationScheme;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private bool _isPreservingHostHeader;
    [ObservableProperty]
    private MatchingRuleKind _newPatternKind;
    [ObservableProperty]
    private string _newPatternText;

    /// <summary>
    ///     Gets the observable collection of entries currently configured on the rule.
    /// </summary>
    public ObservableCollection<MapRemoteEntryViewModel> Entries { get; }

    /// <summary>
    ///     Initializes a new <see cref="MapRemoteViewModel" /> bound to the supplied rule and scheduler.
    /// </summary>
    /// <param name="rule">The mutable map-remote rule.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public MapRemoteViewModel(MutableMapRemoteRule rule, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _rule = rule;
        _userInterfaceScheduler = userInterfaceScheduler;
        _newPatternText = string.Empty;
        _newPatternKind = MatchingRuleKind.Wildcard;
        _destinationScheme = string.Empty;
        _destinationHost = string.Empty;
        _destinationPort = string.Empty;
        _destinationPath = string.Empty;
        _isPreservingHostHeader = false;
        _isEnabled = rule.IsEnabled;
        Entries = [];
        _rule.Changed += OnRuleChanged;
        ReloadEntries();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _rule.Changed -= OnRuleChanged;
    }

    [RelayCommand]
    private void AddEntry()
    {
        var text = NewPatternText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var scheme = string.IsNullOrWhiteSpace(DestinationScheme) ? null : DestinationScheme.Trim();
        var host = string.IsNullOrWhiteSpace(DestinationHost) ? null : DestinationHost.Trim();
        int? port = null;
        if (!string.IsNullOrWhiteSpace(DestinationPort))
        {
            if (!int.TryParse(DestinationPort, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedPort))
            {
                return;
            }

            port = parsedPort;
        }

        var path = string.IsNullOrWhiteSpace(DestinationPath) ? null : DestinationPath.Trim();
        var destination = new MapRemoteDestination(scheme, host, port, path, IsPreservingHostHeader);
        var matchingRule = new MatchingRule(text, NewPatternKind);
        var entry = new MapRemoteEntry
        {
            Destination = destination,
            IsEnabled = true,
            MatchingRule = matchingRule,
        };
        _rule.AddEntry(entry);
        NewPatternText = string.Empty;
        DestinationScheme = string.Empty;
        DestinationHost = string.Empty;
        DestinationPort = string.Empty;
        DestinationPath = string.Empty;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_rule.IsEnabled == value)
        {
            return;
        }

        _rule.SetEnabled(value);
    }

    private void OnRuleChanged()
    {
        _userInterfaceScheduler.Post(ReloadEntries);
    }

    private void ReloadEntries()
    {
        Entries.Clear();
        foreach (var entry in _rule.GetEntries())
        {
            var viewModel = new MapRemoteEntryViewModel(entry);
            Entries.Add(viewModel);
        }

        if (IsEnabled != _rule.IsEnabled)
        {
            IsEnabled = _rule.IsEnabled;
        }
    }

    [RelayCommand]
    private void RemoveEntry(MapRemoteEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _rule.RemoveEntry(entry.Entry);
    }
}
