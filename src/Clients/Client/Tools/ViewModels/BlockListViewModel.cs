using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Block List tool window. Binds to <see cref="MutableBlockListRule" />,
///     exposing the current patterns as an observable collection, an editor for adding new
///     patterns, and commands for adding and removing patterns or toggling the rule on/off.
/// </summary>
public sealed partial class BlockListViewModel : ObservableObject, IDisposable
{
    private readonly MutableBlockListRule _rule;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private MatchingRuleKind _newPatternKind;
    [ObservableProperty]
    private string _newPatternText;

    /// <summary>
    ///     Gets the observable collection of patterns currently configured on the rule.
    /// </summary>
    public ObservableCollection<BlockListPatternViewModel> Patterns { get; }

    /// <summary>
    ///     Initializes a new <see cref="BlockListViewModel" /> bound to the supplied
    ///     mutable rule and UI scheduler.
    /// </summary>
    /// <param name="rule">The domain rule to bind to.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public BlockListViewModel(MutableBlockListRule rule, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _rule = rule;
        _userInterfaceScheduler = userInterfaceScheduler;
        _newPatternText = string.Empty;
        _newPatternKind = MatchingRuleKind.Wildcard;
        _isEnabled = rule.IsEnabled;
        Patterns = [];
        _rule.Changed += OnRuleChanged;
        ReloadPatterns();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _rule.Changed -= OnRuleChanged;
    }

    [RelayCommand]
    private void AddPattern()
    {
        var text = NewPatternText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var rule = new MatchingRule(text, NewPatternKind);
        _rule.AddPattern(rule);
        NewPatternText = string.Empty;
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
        _userInterfaceScheduler.Post(ReloadPatterns);
    }

    private void ReloadPatterns()
    {
        Patterns.Clear();
        foreach (var pattern in _rule.GetPatterns())
        {
            var viewModel = new BlockListPatternViewModel(pattern);
            Patterns.Add(viewModel);
        }

        if (IsEnabled != _rule.IsEnabled)
        {
            IsEnabled = _rule.IsEnabled;
        }
    }

    [RelayCommand]
    private void RemovePattern(BlockListPatternViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _rule.RemovePattern(entry.Rule);
    }
}
