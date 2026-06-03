using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for the source list panel that groups captured traffic
///     by host. Subscribes to <see cref="RequestReceived" /> events and
///     publishes a host-filter request via the shared
///     <see cref="TrafficListCoordinator" /> whenever the selection changes.
/// </summary>
public sealed partial class SourceListViewModel : ObservableObject, IDisposable
{
    private const string AllGroupSentinel = "*";
    private readonly TrafficListCoordinator _coordinator;
    private readonly Dictionary<string, SourceGroupViewModel> _groupsByHost;
    private readonly IDisposable _requestSubscription;
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
    ///     subscribes to the supplied event bus and traffic-list coordinator.
    /// </summary>
    /// <param name="eventBus">The domain event bus to subscribe to.</param>
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

        _coordinator.FlowsChanged += OnFlowsChanged;
        _coordinator.FlowsCleared += OnFlowsCleared;
        _requestSubscription = eventBus.Subscribe<RequestReceived>(OnRequestReceived);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _coordinator.FlowsChanged -= OnFlowsChanged;
        _coordinator.FlowsCleared -= OnFlowsCleared;
        _requestSubscription.Dispose();
    }

    /// <summary>
    ///     Synchronously rebuilds the source list from the current traffic
    ///     flow snapshot, leaving the synthetic "All" group selected.
    /// </summary>
    public void Rebuild()
    {
        _userInterfaceScheduler.Post(RebuildOnUiThread);
    }

    private void OnFlowsChanged()
    {
        Rebuild();
    }

    private void OnFlowsCleared()
    {
        _userInterfaceScheduler.Post(RebuildOnUiThread);
    }

    private void OnRequestReceived(RequestReceived domainEvent)
    {
        var host = SourceHostExtractor.Extract(domainEvent);
        _userInterfaceScheduler.Post(() => RegisterHostOnUiThread(host));
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

    private void RebuildOnUiThread()
    {
        var allGroup = _groupsByHost[AllGroupSentinel];
        allGroup.Count = 0;

        _groupsByHost.Clear();
        _groupsByHost[AllGroupSentinel] = allGroup;
        Sources.Clear();
        Sources.Add(allGroup);

        var flows = _coordinator.GetFlowsSnapshot();
        foreach (var flow in flows)
        {
            var request = flow.Request;
            if (request is null)
            {
                continue;
            }

            var host = SourceHostExtractor.Extract(request);
            RegisterHostOnUiThread(host);
        }

        SelectedSource = allGroup;
    }

    private void RegisterHostOnUiThread(string host)
    {
        var allGroup = _groupsByHost[AllGroupSentinel];
        allGroup.Increment();

        if (!_groupsByHost.TryGetValue(host, out var group))
        {
            var freshGroup = new SourceGroupViewModel(host, false);
            _groupsByHost[host] = freshGroup;
            Sources.Add(freshGroup);
            group = freshGroup;
        }

        group.Increment();
    }
}
