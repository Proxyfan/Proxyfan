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
///     View model for the WebSocket inspector panel. Observes the traffic list
///     selection and, when the selected flow has a corresponding
///     <see cref="WebSocketFlow" /> in the store, surfaces its message stream
///     for display in a Charles/Fiddler-style message list with detail panel.
/// </summary>
public sealed partial class WebSocketInspectorViewModel : ObservableObject, IDisposable
{
    private readonly List<WebSocketMessageViewModel> _allMessages;
    private readonly ObservableCollection<WebSocketMessageViewModel> _messages;
    private readonly IUserInterfaceScheduler _scheduler;
    private readonly TrafficListViewModel _trafficListViewModel;
    private readonly IWebSocketStore _webSocketStore;
    private WebSocketFlow? _attachedFlow;
    [ObservableProperty]
    private string _connectionStatusText;
    [ObservableProperty]
    private WebSocketContentTypeFilter _contentTypeFilter;
    [ObservableProperty]
    private WebSocketDirectionFilter _directionFilter;
    [ObservableProperty]
    private bool _isWebSocket;
    [ObservableProperty]
    private WebSocketMessageViewModel? _selectedMessage;
    [ObservableProperty]
    private string _selectedMessageDetailText;

    /// <summary>
    ///     Gets or sets the content-type filter as a zero-based index suitable for
    ///     binding to a <c>ComboBox.SelectedIndex</c>. Maps to
    ///     <see cref="ContentTypeFilter" /> values in declared order.
    /// </summary>
    public int ContentTypeFilterIndex
    {
        get => (int)ContentTypeFilter;
        set
        {
            var clamped = value < 0 ? 0 : value;
            ContentTypeFilter = (WebSocketContentTypeFilter)clamped;
        }
    }

    /// <summary>
    ///     Gets or sets the direction filter as a zero-based index suitable for
    ///     binding to a <c>ComboBox.SelectedIndex</c>. Maps to
    ///     <see cref="DirectionFilter" /> values in declared order.
    /// </summary>
    public int DirectionFilterIndex
    {
        get => (int)DirectionFilter;
        set
        {
            var clamped = value < 0 ? 0 : value;
            DirectionFilter = (WebSocketDirectionFilter)clamped;
        }
    }

    /// <summary>
    ///     Gets the chronological collection of message rows for the currently
    ///     selected WebSocket flow. Empty when no WebSocket flow is selected.
    /// </summary>
    public ReadOnlyObservableCollection<WebSocketMessageViewModel> Messages { get; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketInspectorViewModel" /> and subscribes
    ///     to traffic-list selection changes.
    /// </summary>
    /// <param name="trafficListViewModel">
    ///     The traffic list view model whose selected flow drives the inspector.
    /// </param>
    /// <param name="webSocketStore">
    ///     The WebSocket flow store used to look up the WebSocket flow associated with
    ///     the selected HTTP flow.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     The UI scheduler used to marshal message-arrival notifications onto the UI
    ///     thread.
    /// </param>
    public WebSocketInspectorViewModel(
        TrafficListViewModel trafficListViewModel,
        IWebSocketStore webSocketStore,
        IUserInterfaceScheduler userInterfaceScheduler)
    {
        _trafficListViewModel = trafficListViewModel;
        _webSocketStore = webSocketStore;
        _scheduler = userInterfaceScheduler;
        var allMessageList = new List<WebSocketMessageViewModel>();
        _allMessages = allMessageList;
        var messageCollection = new ObservableCollection<WebSocketMessageViewModel>();
        _messages = messageCollection;
        var readOnlyMessages = new ReadOnlyObservableCollection<WebSocketMessageViewModel>(_messages);
        Messages = readOnlyMessages;
        _connectionStatusText = string.Empty;
        _contentTypeFilter = WebSocketContentTypeFilter.All;
        _directionFilter = WebSocketDirectionFilter.All;
        _isWebSocket = false;
        _selectedMessage = null;
        _selectedMessageDetailText = string.Empty;
        _attachedFlow = null;
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
    ///     Forces the inspector to re-evaluate the selected flow. Useful when the
    ///     WebSocket store is populated after this view model has been constructed
    ///     and a stale selection still needs to be resolved.
    /// </summary>
    public void Refresh()
    {
        UpdateForSelectedFlow();
    }

    private void AttachFlow(WebSocketFlow flow)
    {
        _attachedFlow = flow;
        flow.MessageRecorded += OnMessageRecorded;
        flow.Closed += OnFlowClosed;

        foreach (var message in flow.Messages)
        {
            var viewModel = new WebSocketMessageViewModel(message);
            _allMessages.Add(viewModel);
        }

        RebuildFilteredMessages();

        ConnectionStatusText = flow.IsClosed
            ? "WebSocket — closed"
            : "WebSocket — open";
    }

    private void DetachCurrentFlow()
    {
        if (_attachedFlow is null)
        {
            return;
        }

        _attachedFlow.MessageRecorded -= OnMessageRecorded;
        _attachedFlow.Closed -= OnFlowClosed;
        _attachedFlow = null;
    }

    private bool HasMatchingFilter(WebSocketMessageViewModel viewModel)
    {
        var message = viewModel.Message;

        if (DirectionFilter == WebSocketDirectionFilter.Outbound &&
            message.Direction != WebSocketDirection.Outbound)
        {
            return false;
        }

        if (DirectionFilter == WebSocketDirectionFilter.Inbound &&
            message.Direction != WebSocketDirection.Inbound)
        {
            return false;
        }

        if (ContentTypeFilter == WebSocketContentTypeFilter.Text &&
            message.Opcode != WebSocketOpcode.Text)
        {
            return false;
        }

        if (ContentTypeFilter == WebSocketContentTypeFilter.Binary &&
            message.Opcode != WebSocketOpcode.Binary)
        {
            return false;
        }

        if (ContentTypeFilter == WebSocketContentTypeFilter.Control &&
            message.Opcode is not WebSocketOpcode.Ping
                          and not WebSocketOpcode.Pong
                          and not WebSocketOpcode.Close)
        {
            return false;
        }

        return true;
    }

    partial void OnContentTypeFilterChanged(WebSocketContentTypeFilter value)
    {
        OnPropertyChanged(nameof(ContentTypeFilterIndex));
        RebuildFilteredMessages();
    }

    partial void OnDirectionFilterChanged(WebSocketDirectionFilter value)
    {
        OnPropertyChanged(nameof(DirectionFilterIndex));
        RebuildFilteredMessages();
    }

    private void OnFlowClosed()
    {
        _scheduler.Post(() =>
        {
            ConnectionStatusText = "WebSocket — closed";
        });
    }

    private void OnMessageRecorded(WebSocketMessage message)
    {
        _scheduler.Post(() =>
        {
            var viewModel = new WebSocketMessageViewModel(message);
            _allMessages.Add(viewModel);
            if (HasMatchingFilter(viewModel))
            {
                _messages.Add(viewModel);
            }
        });
    }

    partial void OnSelectedMessageChanged(WebSocketMessageViewModel? value)
    {
        if (value is null)
        {
            SelectedMessageDetailText = string.Empty;
            return;
        }

        SelectedMessageDetailText = WebSocketPayloadFormatter.FormatFull(value.Message);
    }

    private void OnTrafficListPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName == nameof(TrafficListViewModel.SelectedFlow))
        {
            UpdateForSelectedFlow();
        }
    }

    private void RebuildFilteredMessages()
    {
        _messages.Clear();
        foreach (var viewModel in _allMessages)
        {
            if (HasMatchingFilter(viewModel))
            {
                _messages.Add(viewModel);
            }
        }

        if (SelectedMessage is not null && !_messages.Contains(SelectedMessage))
        {
            SelectedMessage = null;
        }
    }

    private void UpdateForSelectedFlow()
    {
        DetachCurrentFlow();
        _allMessages.Clear();
        _messages.Clear();
        SelectedMessage = null;
        SelectedMessageDetailText = string.Empty;
        ConnectionStatusText = string.Empty;

        var selectedFlow = _trafficListViewModel.SelectedFlow;
        if (selectedFlow is null)
        {
            IsWebSocket = false;
            return;
        }

        var webSocketFlow = _webSocketStore.GetById(selectedFlow.GetDomainFlow().Id);
        if (webSocketFlow is null)
        {
            IsWebSocket = false;
            return;
        }

        IsWebSocket = true;
        AttachFlow(webSocketFlow);
    }
}
