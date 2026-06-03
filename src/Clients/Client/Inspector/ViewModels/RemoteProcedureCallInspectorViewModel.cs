using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using Proxyfan.Framework.Serialization;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model for the Remote Procedure Call (gRPC) inspector panel. Observes the traffic
///     list selection and, when the selected flow has a corresponding
///     <see cref="RemoteProcedureCallFlow" /> in the store, surfaces its message stream for
///     display in a Charles/Fiddler-style message list with detail panel and direction filter.
///     When an <see cref="IRemoteProcedureCallDescriptorLibrary" /> is supplied with a
///     descriptor matching the flow's gRPC method path, payloads are rendered with named
///     fields via <see cref="ProtobufSchemaAwarePrettyPrinter" />.
/// </summary>
public sealed partial class RemoteProcedureCallInspectorViewModel : ObservableObject, IDisposable
{
    private const string DirectionFilterAll = "All";
    private const string DirectionFilterInbound = "Inbound";
    private const string DirectionFilterOutbound = "Outbound";
    private readonly List<RemoteProcedureCallMessageViewModel> _allMessages;
    private readonly IRemoteProcedureCallDescriptorLibrary? _descriptorLibrary;
    private readonly ObservableCollection<RemoteProcedureCallMessageViewModel> _messages;
    private readonly IUserInterfaceScheduler _scheduler;
    private readonly IRemoteProcedureCallStore _store;
    private readonly TrafficListViewModel _trafficListViewModel;
    private RemoteProcedureCallFlow? _attachedFlow;
    [ObservableProperty]
    private string _connectionStatusText;
    [ObservableProperty]
    private string _directionFilter;
    [ObservableProperty]
    private bool _isRemoteProcedureCall;
    [ObservableProperty]
    private RemoteProcedureCallMessageViewModel? _selectedMessage;
    [ObservableProperty]
    private string _selectedMessageDetailText;

    /// <summary>
    ///     Gets the chronological collection of message rows for the currently selected gRPC
    ///     flow. Empty when no gRPC flow is selected.
    /// </summary>
    public ReadOnlyObservableCollection<RemoteProcedureCallMessageViewModel> Messages { get; }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallInspectorViewModel" /> and subscribes
    ///     to traffic-list selection changes. Uses no descriptor library — payloads are decoded
    ///     schema-less.
    /// </summary>
    /// <param name="trafficListViewModel">The traffic list view model.</param>
    /// <param name="store">The gRPC flow store.</param>
    /// <param name="userInterfaceScheduler">The UI scheduler.</param>
    public RemoteProcedureCallInspectorViewModel(
        TrafficListViewModel trafficListViewModel,
        IRemoteProcedureCallStore store,
        IUserInterfaceScheduler userInterfaceScheduler)
        : this(trafficListViewModel, store, userInterfaceScheduler, descriptorLibrary: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="RemoteProcedureCallInspectorViewModel" /> with an
    ///     optional descriptor library for schema-aware decoding.
    /// </summary>
    /// <param name="trafficListViewModel">The traffic list view model.</param>
    /// <param name="store">The gRPC flow store.</param>
    /// <param name="userInterfaceScheduler">The UI scheduler.</param>
    /// <param name="descriptorLibrary">Optional descriptor library; when supplied and the
    ///     selected gRPC flow's method path resolves to a method descriptor, payloads are
    ///     rendered with named fields.</param>
    public RemoteProcedureCallInspectorViewModel(
        TrafficListViewModel trafficListViewModel,
        IRemoteProcedureCallStore store,
        IUserInterfaceScheduler userInterfaceScheduler,
        IRemoteProcedureCallDescriptorLibrary? descriptorLibrary)
    {
        _trafficListViewModel = trafficListViewModel;
        _store = store;
        _scheduler = userInterfaceScheduler;
        _descriptorLibrary = descriptorLibrary;
        var allMessagesList = new List<RemoteProcedureCallMessageViewModel>();
        _allMessages = allMessagesList;
        var messageCollection = new ObservableCollection<RemoteProcedureCallMessageViewModel>();
        _messages = messageCollection;
        var readOnlyMessages = new ReadOnlyObservableCollection<RemoteProcedureCallMessageViewModel>(_messages);
        Messages = readOnlyMessages;
        _connectionStatusText = string.Empty;
        _directionFilter = DirectionFilterAll;
        _isRemoteProcedureCall = false;
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
    ///     Forces the inspector to re-evaluate the selected flow.
    /// </summary>
    public void Refresh()
    {
        UpdateForSelectedFlow();
    }

    private void AttachFlow(RemoteProcedureCallFlow flow)
    {
        _attachedFlow = flow;
        flow.MessageRecorded += OnMessageRecorded;
        flow.Closed += OnFlowClosed;

        foreach (var capturedMessage in flow.Messages)
        {
            var viewModel = new RemoteProcedureCallMessageViewModel(capturedMessage);
            _allMessages.Add(viewModel);
        }

        RebuildFilteredMessages();

        ConnectionStatusText = flow.IsClosed
            ? "gRPC — closed"
            : "gRPC — streaming";
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

    private bool HasMatchingFilter(RemoteProcedureCallMessageViewModel viewModel)
    {
        if (string.Equals(DirectionFilter, DirectionFilterInbound, StringComparison.Ordinal))
        {
            return viewModel.CapturedMessage.Direction == RemoteProcedureCallDirection.Inbound;
        }

        if (string.Equals(DirectionFilter, DirectionFilterOutbound, StringComparison.Ordinal))
        {
            return viewModel.CapturedMessage.Direction == RemoteProcedureCallDirection.Outbound;
        }

        return true;
    }

    partial void OnDirectionFilterChanged(string value)
    {
        _ = value;
        RebuildFilteredMessages();
    }

    private void OnFlowClosed()
    {
        _scheduler.Post(() =>
        {
            ConnectionStatusText = "gRPC — closed";
        });
    }

    private void OnMessageRecorded(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        _scheduler.Post(() =>
        {
            var viewModel = new RemoteProcedureCallMessageViewModel(capturedMessage);
            _allMessages.Add(viewModel);
            if (HasMatchingFilter(viewModel))
            {
                _messages.Add(viewModel);
            }
        });
    }

    partial void OnSelectedMessageChanged(RemoteProcedureCallMessageViewModel? value)
    {
        if (value is null)
        {
            SelectedMessageDetailText = string.Empty;
            return;
        }

        var resolution = ResolveSchema(value.CapturedMessage);
        SelectedMessageDetailText = RemoteProcedureCallPayloadFormatter.FormatFull(value.CapturedMessage, resolution.Schema, resolution.Index);
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

    private RemoteProcedureCallSchemaResolution ResolveSchema(RemoteProcedureCallCapturedMessage capturedMessage)
    {
        if (_descriptorLibrary is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var methodPath = _attachedFlow?.MethodPath;
        if (methodPath is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var index = _descriptorLibrary.Index;
        var method = index.TryResolveMethod(methodPath);
        if (method is null)
        {
            return new RemoteProcedureCallSchemaResolution();
        }

        var typeName = capturedMessage.Direction == RemoteProcedureCallDirection.Outbound
            ? method.InputType
            : method.OutputType;
        var schema = index.TryResolveMessage(typeName);
        var resolution = new RemoteProcedureCallSchemaResolution
        {
            Index = index,
            Schema = schema,
        };
        return resolution;
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
            IsRemoteProcedureCall = false;
            return;
        }

        var remoteProcedureCallFlow = _store.GetById(selectedFlow.Id);
        if (remoteProcedureCallFlow is null)
        {
            IsRemoteProcedureCall = false;
            return;
        }

        IsRemoteProcedureCall = true;
        AttachFlow(remoteProcedureCallFlow);
    }
}
