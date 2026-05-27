using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Rules.Matching;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Breakpoint tool window. Binds to
///     <see cref="MutableBreakpointConfiguration" /> and <see cref="IBreakpointPauseInbox" />
///     to expose three regions: configuration (enabled/phases/patterns), pending pauses,
///     and an editor for the currently-selected pause.
/// </summary>
public sealed partial class BreakpointViewModel : ObservableObject, IDisposable
{
    private readonly MutableBreakpointConfiguration _configuration;
    private readonly IBreakpointPauseInbox _inbox;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private bool _isEnabled;
    [ObservableProperty]
    private MatchingRuleKind _newPatternKind;
    [ObservableProperty]
    private string _newPatternText;
    [ObservableProperty]
    private BreakpointPhase _phases;
    [ObservableProperty]
    private BreakpointPauseViewModel? _selectedPause;

    /// <summary>
    ///     Gets the observable collection of URL patterns currently configured.
    /// </summary>
    public ObservableCollection<BlockListPatternViewModel> Patterns { get; }

    /// <summary>
    ///     Gets the observable collection of pending pauses awaiting user resolution.
    /// </summary>
    public ObservableCollection<BreakpointPauseViewModel> Pauses { get; }

    /// <summary>
    ///     Initializes a new <see cref="BreakpointViewModel" /> bound to the supplied domain types.
    /// </summary>
    /// <param name="configuration">Mutable breakpoint configuration to bind to.</param>
    /// <param name="inbox">Inbox of pending pauses produced by the breakpoint handler.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public BreakpointViewModel(
        MutableBreakpointConfiguration configuration,
        IBreakpointPauseInbox inbox,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _configuration = configuration;
        _inbox = inbox;
        _userInterfaceScheduler = userInterfaceScheduler;
        _newPatternText = string.Empty;
        _newPatternKind = MatchingRuleKind.Wildcard;
        _isEnabled = configuration.IsEnabled;
        _phases = configuration.Phases;
        Patterns = [];
        Pauses = [];
        _configuration.Changed += OnConfigurationChanged;
        _inbox.PauseAdded += OnPauseAdded;
        _inbox.PauseResolved += OnPauseResolved;
        ReloadPatterns();
        ReloadPauses();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _configuration.Changed -= OnConfigurationChanged;
        _inbox.PauseAdded -= OnPauseAdded;
        _inbox.PauseResolved -= OnPauseResolved;
    }

    [RelayCommand]
    private void Abort(BreakpointPauseViewModel? pause)
    {
        if (pause is null)
        {
            return;
        }

        _inbox.Abort(pause.Pause);
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
        _configuration.AddPattern(rule);
        NewPatternText = string.Empty;
    }

    private void OnConfigurationChanged()
    {
        _userInterfaceScheduler.Post(ReloadPatterns);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_configuration.IsEnabled == value)
        {
            return;
        }

        _configuration.SetEnabled(value);
    }

    private void OnPauseAdded(BreakpointPause pause)
    {
        _userInterfaceScheduler.Post(() =>
        {
            var viewModel = new BreakpointPauseViewModel(pause);
            Pauses.Add(viewModel);
            SelectedPause ??= viewModel;
        });
    }

    private void OnPauseResolved(BreakpointPause pause)
    {
        _userInterfaceScheduler.Post(() =>
        {
            for (var index = Pauses.Count - 1; index >= 0; index--)
            {
                if (Pauses[index].Pause.PauseId == pause.PauseId)
                {
                    if (ReferenceEquals(SelectedPause, Pauses[index]))
                    {
                        SelectedPause = null;
                    }

                    Pauses.RemoveAt(index);
                    return;
                }
            }
        });
    }

    partial void OnPhasesChanged(BreakpointPhase value)
    {
        if (_configuration.Phases == value)
        {
            return;
        }

        _configuration.SetPhases(value);
    }

    private void ReloadPatterns()
    {
        Patterns.Clear();
        foreach (var pattern in _configuration.GetPatterns())
        {
            var viewModel = new BlockListPatternViewModel(pattern);
            Patterns.Add(viewModel);
        }

        if (IsEnabled != _configuration.IsEnabled)
        {
            IsEnabled = _configuration.IsEnabled;
        }

        if (Phases != _configuration.Phases)
        {
            Phases = _configuration.Phases;
        }
    }

    private void ReloadPauses()
    {
        Pauses.Clear();
        foreach (var pending in _inbox.GetPending())
        {
            var viewModel = new BreakpointPauseViewModel(pending);
            Pauses.Add(viewModel);
        }

        if (SelectedPause is null && Pauses.Count > 0)
        {
            SelectedPause = Pauses[0];
        }
    }

    [RelayCommand]
    private void RemovePattern(BlockListPatternViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        _configuration.RemovePattern(entry.Rule);
    }

    [RelayCommand]
    private void Resume(BreakpointPauseViewModel? pause)
    {
        if (pause is null)
        {
            return;
        }

        BreakpointDecision decision;
        if (pause.Phase == BreakpointPhase.Request)
        {
            decision = pause.BuildRequestDecision();
        }
        else
        {
            decision = pause.BuildResponseDecision();
        }

        _inbox.Resolve(pause.Pause, decision);
    }
}
