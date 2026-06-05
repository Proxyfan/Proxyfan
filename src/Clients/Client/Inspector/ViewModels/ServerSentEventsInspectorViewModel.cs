using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model for the Server-Sent Events (SSE) inspector panel. Observes the traffic
///     list selection and, when the selected flow has a corresponding
///     <see cref="ServerSentEventsFlow" /> in the store, surfaces its event stream for
///     display in a Charles/Fiddler-style event list with detail panel.
/// </summary>
public sealed partial class ServerSentEventsInspectorViewModel : ObservableObject, IDisposable
{
    private readonly List<ServerSentEventViewModel> _allEvents;
    private readonly ObservableCollection<ServerSentEventViewModel> _events;
    private readonly IUserInterfaceScheduler _scheduler;
    private readonly IServerSentEventsStore _store;
    private readonly TrafficListViewModel _trafficListViewModel;
    private ServerSentEventsFlow? _attachedFlow;
    private HashSet<ServerSentEvent>? _attachedFlowSnapshotSet;
    private int _attachmentGeneration;
    [ObservableProperty]
    private string _connectionStatusText;
    [ObservableProperty]
    private string _eventTypeFilter;
    [ObservableProperty]
    private bool _isServerSentEvents;
    [ObservableProperty]
    private ServerSentEventViewModel? _selectedEvent;
    [ObservableProperty]
    private string _selectedEventDetailText;

    /// <summary>
    ///     Gets the chronological collection of event rows for the currently selected SSE
    ///     flow. Empty when no SSE flow is selected.
    /// </summary>
    public ReadOnlyObservableCollection<ServerSentEventViewModel> Events { get; }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventsInspectorViewModel" /> and subscribes
    ///     to traffic-list selection changes.
    /// </summary>
    /// <param name="trafficListViewModel">The traffic list view model.</param>
    /// <param name="store">The SSE flow store.</param>
    /// <param name="userInterfaceScheduler">The UI scheduler.</param>
    public ServerSentEventsInspectorViewModel(
        TrafficListViewModel trafficListViewModel,
        IServerSentEventsStore store,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _trafficListViewModel = trafficListViewModel;
        _store = store;
        _scheduler = userInterfaceScheduler;
        var allEventsList = new List<ServerSentEventViewModel>();
        _allEvents = allEventsList;
        var eventCollection = new ObservableCollection<ServerSentEventViewModel>();
        _events = eventCollection;
        var readOnlyEvents = new ReadOnlyObservableCollection<ServerSentEventViewModel>(_events);
        Events = readOnlyEvents;
        _connectionStatusText = string.Empty;
        _eventTypeFilter = string.Empty;
        _isServerSentEvents = false;
        _selectedEvent = null;
        _selectedEventDetailText = string.Empty;
        _attachedFlow = null;
        _attachedFlowSnapshotSet = null;
        _trafficListViewModel.PropertyChanged += OnTrafficListPropertyChanged;
        Refresh();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _trafficListViewModel.PropertyChanged -= OnTrafficListPropertyChanged;
        DetachCurrentFlow();
    }

    /// <summary>
    ///     Forces the inspector to re-evaluate the selected flow.
    /// </summary>
    public void Refresh()
    {
        UpdateForSelectedFlow();
    }

    private void AttachFlow(ServerSentEventsFlow flow)
    {
        _attachedFlow = flow;
        flow.EventRecorded += OnEventRecorded;
        flow.Closed += OnFlowClosed;

        var snapshot = flow.GetEventsSnapshot();
        var snapshotSet = new HashSet<ServerSentEvent>(snapshot.Events, ReferenceEqualityComparer.Instance);
        _attachedFlowSnapshotSet = snapshotSet;

        foreach (var serverSentEvent in snapshot.Events)
        {
            var viewModel = new ServerSentEventViewModel(serverSentEvent);
            _allEvents.Add(viewModel);
        }

        RebuildFilteredEvents();

        ConnectionStatusText = snapshot.IsClosed
            ? "Server-Sent Events — closed"
            : "Server-Sent Events — streaming";
    }

    private void DetachCurrentFlow()
    {
        if (_attachedFlow is null)
        {
            return;
        }

        _attachedFlow.EventRecorded -= OnEventRecorded;
        _attachedFlow.Closed -= OnFlowClosed;
        _attachedFlow = null;
        _attachedFlowSnapshotSet = null;
        _attachmentGeneration++;
    }

    private bool HasMatchingFilter(ServerSentEventViewModel viewModel)
    {
        if (string.IsNullOrEmpty(EventTypeFilter))
        {
            return true;
        }

        var eventType = viewModel.ServerSentEvent.EventType ?? string.Empty;
        return eventType.Contains(EventTypeFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void OnEventRecorded(ServerSentEvent serverSentEvent)
    {
        var capturedGeneration = _attachmentGeneration;
        _scheduler.Post(() =>
        {
            if (_attachmentGeneration != capturedGeneration)
            {
                return;
            }

            if (_attachedFlowSnapshotSet?.Remove(serverSentEvent) == true)
            {
                return;
            }

            var viewModel = new ServerSentEventViewModel(serverSentEvent);
            _allEvents.Add(viewModel);
            if (HasMatchingFilter(viewModel))
            {
                _events.Add(viewModel);
            }
        });
    }

    partial void OnEventTypeFilterChanged(string value)
    {
        _ = value;
        RebuildFilteredEvents();
    }

    private void OnFlowClosed()
    {
        var capturedGeneration = _attachmentGeneration;
        _scheduler.Post(() =>
        {
            if (_attachmentGeneration != capturedGeneration)
            {
                return;
            }

            ConnectionStatusText = "Server-Sent Events — closed";
        });
    }

    partial void OnSelectedEventChanged(ServerSentEventViewModel? value)
    {
        if (value is null)
        {
            SelectedEventDetailText = string.Empty;
            return;
        }

        SelectedEventDetailText = ServerSentEventPayloadFormatter.FormatFull(value.ServerSentEvent);
    }

    private void OnTrafficListPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName == nameof(TrafficListViewModel.SelectedFlow))
        {
            UpdateForSelectedFlow();
        }
    }

    private void RebuildFilteredEvents()
    {
        _events.Clear();
        foreach (var viewModel in _allEvents)
        {
            if (HasMatchingFilter(viewModel))
            {
                _events.Add(viewModel);
            }
        }

        if (SelectedEvent is not null && !_events.Contains(SelectedEvent))
        {
            SelectedEvent = null;
        }
    }

    private void UpdateForSelectedFlow()
    {
        DetachCurrentFlow();
        _allEvents.Clear();
        _events.Clear();
        SelectedEvent = null;
        SelectedEventDetailText = string.Empty;
        ConnectionStatusText = string.Empty;

        var selectedFlow = _trafficListViewModel.SelectedFlow;
        if (selectedFlow is null)
        {
            IsServerSentEvents = false;
            return;
        }

        var sseFlow = _store.GetById(selectedFlow.Id);
        if (sseFlow is null)
        {
            IsServerSentEvents = false;
            return;
        }

        IsServerSentEvents = true;
        AttachFlow(sseFlow);
    }
}
