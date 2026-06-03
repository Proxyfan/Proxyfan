using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for the source list panel that groups captured traffic
///     by host and publishes a host-filter request via the shared
///     <see cref="TrafficListCoordinator" /> whenever the selection changes.
/// </summary>
public sealed partial class SourceListViewModel : ObservableObject, IDisposable
{
    private const string AllGroupSentinel = "*";
    private readonly TrafficListCoordinator _coordinator;
    private readonly Dictionary<string, SourceGroupViewModel> _groupsByHost;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private SourceGroupViewModel? _selectedSource;

    /// <summary>
    ///     Gets the observable collection of source groups (one per host)
    ///     for binding to the source list view.
    /// </summary>
    public ObservableCollection<SourceGroupViewModel> Sources { get; }

    /// <summary>
    ///     Initializes a new <see cref="SourceListViewModel" /> and
    ///     subscribes to traffic-list coordinator notifications.
    /// </summary>
    /// <param name="eventBus">
    ///     The domain event bus.
    /// </param>
    /// <param name="coordinator">
    ///     Shared mediator used to publish host-filter requests and to
    ///     observe flows-cleared notifications from the traffic list.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     Scheduler used to marshal collection mutations onto the UI thread.
    /// </param>
    public SourceListViewModel(
        IDomainEventBus eventBus,
        TrafficListCoordinator coordinator,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _ = eventBus;
        _coordinator = coordinator;
        _userInterfaceScheduler = userInterfaceScheduler;
        var groupsByHost = new Dictionary<string, SourceGroupViewModel>(StringComparer.OrdinalIgnoreCase);
        _groupsByHost = groupsByHost;

        var allGroup = new SourceGroupViewModel(AllGroupSentinel, true);
        var sources = new ObservableCollection<SourceGroupViewModel>
        {
            allGroup,
        };
        Sources = sources;
        _groupsByHost[AllGroupSentinel] = allGroup;
        _selectedSource = allGroup;

        _coordinator.FlowsCleared += OnFlowsCleared;
        _coordinator.SourceHostsUpdated += OnSourceHostsUpdated;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _coordinator.FlowsCleared -= OnFlowsCleared;
        _coordinator.SourceHostsUpdated -= OnSourceHostsUpdated;
    }

    /// <summary>
    ///     Synchronously rebuilds the source list, leaving only the
    ///     synthetic "All" group selected.
    /// </summary>
    public void Rebuild()
    {
        _userInterfaceScheduler.Post(RebuildOnUiThread);
    }

    private void OnFlowsCleared()
    {
        _userInterfaceScheduler.Post(RebuildOnUiThread);
    }

    partial void OnSelectedSourceChanged(SourceGroupViewModel? value)
    {
        if (value is null || value.IsAllGroup)
        {
            _coordinator.RequestHostFilter(string.Empty);
            return;
        }

        _coordinator.RequestHostFilter(value.Host);
    }

    private void OnSourceHostsUpdated(IReadOnlyDictionary<string, int> hostCounts)
    {
        _userInterfaceScheduler.Post(() => RebuildOnUiThread(hostCounts));
    }

    private void RebuildOnUiThread()
    {
        var allGroup = _groupsByHost[AllGroupSentinel];
        allGroup.Count = 0;

        _groupsByHost.Clear();
        _groupsByHost[AllGroupSentinel] = allGroup;
        Sources.Clear();
        Sources.Add(allGroup);
        SelectedSource = allGroup;
    }

    private void RebuildOnUiThread(IReadOnlyDictionary<string, int> hostCounts)
    {
        var allGroup = _groupsByHost[AllGroupSentinel];
        _groupsByHost.Clear();
        _groupsByHost[AllGroupSentinel] = allGroup;
        Sources.Clear();
        Sources.Add(allGroup);

        var totalFlowCount = 0;
        foreach (var hostCount in hostCounts)
        {
            totalFlowCount += hostCount.Value;
            var group = new SourceGroupViewModel(hostCount.Key, false)
            {
                Count = hostCount.Value,
            };

            _groupsByHost[hostCount.Key] = group;
            Sources.Add(group);
        }

        allGroup.Count = totalFlowCount;
        SelectedSource = allGroup;
    }

}
