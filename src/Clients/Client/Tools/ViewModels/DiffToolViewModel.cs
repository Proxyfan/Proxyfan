using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Traffic.Diff;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Diff Tool window. Subscribes to a
///     <see cref="TrafficFlowDiffPool" /> for the available flows, lets the user
///     select left and right candidates, computes a <see cref="TrafficFlowDiff" />
///     between them, and renders the result as unified-diff text.
/// </summary>
public sealed partial class DiffToolViewModel : ObservableObject, IDisposable
{
    private readonly Dictionary<Guid, DiffPoolItemViewModel> _items;
    private readonly TrafficFlowDiffPool _pool;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _diffText;
    [ObservableProperty]
    private bool _isIdentical;
    [ObservableProperty]
    private DiffPoolItemViewModel? _leftFlow;
    [ObservableProperty]
    private DiffPoolItemViewModel? _rightFlow;

    /// <summary>
    ///     Gets the flows currently in the diff pool.
    /// </summary>
    public ObservableCollection<DiffPoolItemViewModel> Flows { get; }

    /// <summary>
    ///     Initializes a new <see cref="DiffToolViewModel" />.
    /// </summary>
    /// <param name="pool">The shared diff pool the user adds flows into.</param>
    /// <param name="userInterfaceScheduler">Scheduler used to marshal updates onto the UI thread.</param>
    public DiffToolViewModel(TrafficFlowDiffPool pool, IUserInterfaceScheduler userInterfaceScheduler)
    {
        _pool = pool;
        _userInterfaceScheduler = userInterfaceScheduler;
        var items = new Dictionary<Guid, DiffPoolItemViewModel>();
        _items = items;
        _diffText = string.Empty;
        _isIdentical = false;
        Flows = [];
        _pool.Changed += OnPoolChanged;
        ReloadFlows();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pool.Changed -= OnPoolChanged;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (string.Equals(e.PropertyName, nameof(LeftFlow), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(RightFlow), StringComparison.Ordinal))
        {
            RecomputeDiff();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _pool.Clear();
    }

    private void OnPoolChanged(TrafficFlowDiffPool pool)
    {
        _userInterfaceScheduler.Post(ReloadFlows);
    }

    private void RecomputeDiff()
    {
        if (LeftFlow is null || RightFlow is null)
        {
            DiffText = string.Empty;
            IsIdentical = false;
            return;
        }

        var diff = TrafficFlowDiffer.Diff(LeftFlow.Flow, RightFlow.Flow);
        IsIdentical = diff.IsIdentical;
        DiffText = UnifiedDiffFormatter.Format(diff);
    }

    private void ReloadFlows()
    {
        var snapshot = _pool.Snapshot();
        var seen = new HashSet<Guid>();
        foreach (var flow in snapshot)
        {
            seen.Add(flow.Id);
            if (_items.ContainsKey(flow.Id))
            {
                continue;
            }

            var item = new DiffPoolItemViewModel(flow);
            _items[flow.Id] = item;
            Flows.Add(item);
        }

        for (var index = Flows.Count - 1; index >= 0; index--)
        {
            var current = Flows[index];
            if (!seen.Contains(current.Flow.Id))
            {
                Flows.RemoveAt(index);
                _items.Remove(current.Flow.Id);
                if (ReferenceEquals(LeftFlow, current))
                {
                    LeftFlow = null;
                }

                if (ReferenceEquals(RightFlow, current))
                {
                    RightFlow = null;
                }
            }
        }
    }

    [RelayCommand]
    private void Remove(DiffPoolItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _pool.Remove(item.Flow);
    }
}
