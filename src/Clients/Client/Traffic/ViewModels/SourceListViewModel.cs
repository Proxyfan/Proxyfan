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
///     publishes a host filter through the <see cref="ITrafficListCoordinator" />
///     abstraction whenever the selection changes. The traffic list view
///     model consumes that same coordinator, so neither view model needs to
///     reference the other directly.
/// </summary>
public sealed partial class SourceListViewModel : ObservableObject, IDisposable
{
    private const string AllGroupSentinel = "*";
    private readonly ITrafficListCoordinator _coordinator;
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
    ///     subscribes to the supplied event bus.
    /// </summary>
    /// <param name="eventBus">The domain event bus to subscribe to.</param>
    /// <param name="coordinator">
    ///     The coordinator that brokers host-filter selections and
    ///     flows-reset signals between this view model and the traffic
    ///     list view model.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     Scheduler used to marshal collection mutations onto the UI thread.
    /// </param>
    public SourceListViewModel(
        IDomainEventBus eventBus,
        ITrafficListCoordinator coordinator,
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

        _coordinator.FlowsReset += OnFlowsReset;
        _requestSubscription = eventBus.Subscribe<RequestReceived>(OnRequestReceived);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _coordinator.FlowsReset -= OnFlowsReset;
        _requestSubscription.Dispose();
    }

    /// <summary>
    ///     Synchronously rebuilds the source list, clearing every host
    ///     group except the "All" sentinel.
    /// </summary>
    public void Rebuild()
    {
        _userInterfaceScheduler.Post(RebuildOnUiThread);
    }

    private void OnFlowsReset()
    {
        RebuildOnUiThread();
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
            _coordinator.HostFilter = string.Empty;
            return;
        }

        _coordinator.HostFilter = value.Host;
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
