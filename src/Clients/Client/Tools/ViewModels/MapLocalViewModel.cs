using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Map Local tool window. Binds to <see cref="MutableMapLocalRule" />
///     and exposes the current entries as an observable collection plus an editor for
///     adding new entries (status, reason, body, optional headers as 'Name: Value' lines).
/// </summary>
public sealed partial class MapLocalViewModel : ObservableObject, IDisposable
{
    private readonly MutableMapLocalRule _rule;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private MatchingRuleKind _newPatternKind;
    [ObservableProperty]
    private string _newPatternText;
    [ObservableProperty]
    private string _responseBody;
    [ObservableProperty]
    private string _responseHeaders;
    [ObservableProperty]
    private string _responseReasonPhrase;
    [ObservableProperty]
    private string _responseStatusCode;
    [ObservableProperty]
    private string? _validationMessage;

    /// <summary>
    ///     Gets the observable collection of entries currently configured on the rule.
    /// </summary>
    public ObservableCollection<MapLocalEntryViewModel> Entries { get; }

    /// <summary>
    ///     Initializes a new <see cref="MapLocalViewModel" /> bound to the supplied rule and scheduler.
    /// </summary>
    /// <param name="rule">The mutable map-local rule.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public MapLocalViewModel(MutableMapLocalRule rule, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _rule = rule;
        _userInterfaceScheduler = userInterfaceScheduler;
        _newPatternText = string.Empty;
        _newPatternKind = MatchingRuleKind.Wildcard;
        _responseStatusCode = "200";
        _responseReasonPhrase = "OK";
        _responseHeaders = string.Empty;
        _responseBody = string.Empty;
        _validationMessage = null;
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

        if (!int.TryParse(ResponseStatusCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var statusCode))
        {
            return;
        }

        if (statusCode is < 100 or > 599)
        {
            return;
        }

        var headers = MapLocalHeaderParser.Parse(ResponseHeaders);
        var body = Encoding.UTF8.GetBytes(ResponseBody);
        var matchingRule = new MatchingRule(text, NewPatternKind);
        var entry = new MapLocalEntry
        {
            Body = body,
            Headers = headers,
            IsEnabled = true,
            MatchingRule = matchingRule,
            ReasonPhrase = ResponseReasonPhrase,
            StatusCode = statusCode,
        };
        try
        {
            _rule.AddEntry(entry);
        }
        catch (RegexParseException exception)
        {
            ValidationMessage = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            ValidationMessage = exception.Message;
            return;
        }

        ValidationMessage = null;
        NewPatternText = string.Empty;
        ResponseBody = string.Empty;
        ResponseHeaders = string.Empty;
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
            var viewModel = new MapLocalEntryViewModel(entry);
            Entries.Add(viewModel);
        }

        if (IsEnabled != _rule.IsEnabled)
        {
            IsEnabled = _rule.IsEnabled;
        }
    }

    [RelayCommand]
    private void RemoveEntry(MapLocalEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _rule.RemoveEntry(entry.Entry);
    }
}
