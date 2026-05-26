using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for the traffic flow list. Subscribes to domain events and
///     maintains the observable collection of captured flows.
/// </summary>
public sealed partial class TrafficListViewModel : ObservableObject, IDisposable
{
    private readonly ConcurrentDictionary<Guid, TrafficFlowViewModel> _flowById;
    private readonly IDisposable _flowCompletedSubscription;
    private readonly IDisposable _requestReceivedSubscription;
    private readonly IDisposable _responseReceivedSubscription;
    private int _nextNumber;
    [ObservableProperty]
    private TrafficFlowViewModel? _selectedFlow;

    /// <summary>
    ///     Gets the observable collection of captured traffic flows.
    /// </summary>
    public ObservableCollection<TrafficFlowViewModel> Flows { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> and subscribes to capture events.
    /// </summary>
    /// <param name="eventBus">
    ///     The domain event bus used to subscribe to traffic events.
    /// </param>
    public TrafficListViewModel(IDomainEventBus eventBus)
    {
        var flowById = new ConcurrentDictionary<Guid, TrafficFlowViewModel>();
        _flowById = flowById;

        var flows = new ObservableCollection<TrafficFlowViewModel>();
        Flows = flows;

        _requestReceivedSubscription = eventBus.Subscribe<RequestReceived>(OnRequestReceived);
        _responseReceivedSubscription = eventBus.Subscribe<ResponseReceived>(OnResponseReceived);
        _flowCompletedSubscription = eventBus.Subscribe<TrafficFlowCompleted>(OnFlowCompleted);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _requestReceivedSubscription.Dispose();
        _responseReceivedSubscription.Dispose();
        _flowCompletedSubscription.Dispose();
    }

    private void OnFlowCompleted(TrafficFlowCompleted domainEvent)
    {
        if (!_flowById.TryGetValue(domainEvent.TrafficFlowId, out var viewModel))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => viewModel.UpdateStatus(domainEvent));
    }

    private void OnRequestReceived(RequestReceived domainEvent)
    {
        var number = Interlocked.Increment(ref _nextNumber);
        var viewModel = new TrafficFlowViewModel(domainEvent, number);
        _flowById.TryAdd(domainEvent.TrafficFlowId, viewModel);

        Dispatcher.UIThread.Post(() => Flows.Add(viewModel));
    }

    private void OnResponseReceived(ResponseReceived domainEvent)
    {
        if (!_flowById.TryGetValue(domainEvent.TrafficFlowId, out var viewModel))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => viewModel.UpdateResponse(domainEvent));
    }
}