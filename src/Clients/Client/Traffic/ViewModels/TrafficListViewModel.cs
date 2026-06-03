using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Diff;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Traffic.ViewModels;

/// <summary>
///     View model for the traffic flow list. Subscribes to domain events and
///     maintains the observable collection of captured flows.
/// </summary>
public sealed partial class TrafficListViewModel : ObservableObject, IDisposable
{
    private readonly Proxyfan.Presentation.Clipboard.IClipboardService? _clipboardService;
    private readonly TrafficListCoordinator _coordinator;
    private readonly TrafficFlowDiffPool? _diffPool;
    private readonly ConcurrentDictionary<Guid, TrafficFlowViewModel> _flowById;
    private readonly IDisposable _flowCompletedSubscription;
    private readonly IDisposable _requestReceivedSubscription;
    private readonly IRequestRepeater? _requestRepeater;
    private readonly IDisposable _responseReceivedSubscription;
    private readonly IUserInterfaceScheduler _userInterfaceScheduler;
    [ObservableProperty]
    private string _filterText;
    [ObservableProperty]
    private string _hostFilter;
    [ObservableProperty]
    private bool _isCapturing;
    private int _nextNumber;
    [ObservableProperty]
    private TrafficFlowViewModel? _selectedFlow;

    /// <summary>
    ///     Gets the unfiltered observable collection of all captured traffic flows.
    /// </summary>
    public ObservableCollection<TrafficFlowViewModel> Flows { get; }

    /// <summary>
    ///     Gets the filtered observable collection of traffic flows to display in the UI.
    ///     Rebuilt automatically whenever <see cref="Flows" /> or <see cref="FilterText" /> changes.
    /// </summary>
    public ObservableCollection<TrafficFlowViewModel> VisibleFlows { get; }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> and subscribes to capture events.
    /// </summary>
    /// <param name="eventBus">
    ///     The domain event bus used to subscribe to traffic events.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     Scheduler used to marshal collection mutations onto the UI thread.
    /// </param>
    public TrafficListViewModel(IDomainEventBus eventBus, IUserInterfaceScheduler userInterfaceScheduler)
        : this(eventBus, userInterfaceScheduler, requestRepeater: null, diffPool: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> with an explicit request repeater
    ///     so the &quot;Repeat&quot; context-menu action can replay captured flows through the proxy
    ///     pipeline.
    /// </summary>
    /// <param name="eventBus">
    ///     The domain event bus used to subscribe to traffic events.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     Scheduler used to marshal collection mutations onto the UI thread.
    /// </param>
    /// <param name="requestRepeater">
    ///     Optional request repeater. When <see langword="null" /> the Repeat commands no-op
    ///     (used by tests that exercise the list without spinning up the network stack).
    /// </param>
    public TrafficListViewModel(
        IDomainEventBus eventBus,
        IUserInterfaceScheduler userInterfaceScheduler,
        IRequestRepeater? requestRepeater)
        : this(eventBus, userInterfaceScheduler, requestRepeater, diffPool: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> with a request repeater and a
    ///     shared diff pool so the &quot;Add to Diff Pool&quot; context-menu action can stash the
    ///     selected flow for later side-by-side comparison in the Diff Tool window.
    /// </summary>
    /// <param name="eventBus">
    ///     The domain event bus used to subscribe to traffic events.
    /// </param>
    /// <param name="userInterfaceScheduler">
    ///     Scheduler used to marshal collection mutations onto the UI thread.
    /// </param>
    /// <param name="requestRepeater">
    ///     Optional request repeater. When <see langword="null" /> the Repeat commands no-op.
    /// </param>
    /// <param name="diffPool">
    ///     Optional shared diff pool. When <see langword="null" /> the
    ///     <see cref="AddSelectedToDiffPoolCommand" /> no-ops.
    /// </param>
    public TrafficListViewModel(
        IDomainEventBus eventBus,
        IUserInterfaceScheduler userInterfaceScheduler,
        IRequestRepeater? requestRepeater,
        TrafficFlowDiffPool? diffPool)
        : this(eventBus, userInterfaceScheduler, requestRepeater, diffPool, clipboardService: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> with a request repeater, a
    ///     shared diff pool, and a clipboard service so the &quot;Copy URL&quot;,
    ///     &quot;Copy as cURL&quot;, and &quot;Copy as Raw HTTP&quot; context-menu actions can
    ///     publish text to the system clipboard.
    /// </summary>
    /// <param name="eventBus">The domain event bus.</param>
    /// <param name="userInterfaceScheduler">The UI scheduler.</param>
    /// <param name="requestRepeater">Optional request repeater.</param>
    /// <param name="diffPool">Optional shared diff pool.</param>
    /// <param name="clipboardService">Optional clipboard service used by the copy commands.</param>
    public TrafficListViewModel(
        IDomainEventBus eventBus,
        IUserInterfaceScheduler userInterfaceScheduler,
        IRequestRepeater? requestRepeater,
        TrafficFlowDiffPool? diffPool,
        Proxyfan.Presentation.Clipboard.IClipboardService? clipboardService)
        : this(eventBus, userInterfaceScheduler, requestRepeater, diffPool, clipboardService, coordinator: null)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="TrafficListViewModel" /> with the
    ///     full set of optional collaborators including the shared
    ///     <see cref="TrafficListCoordinator" /> through which sibling view
    ///     models (e.g. the source-list panel) request host-filter changes
    ///     and observe flows-cleared notifications.
    /// </summary>
    /// <param name="eventBus">The domain event bus.</param>
    /// <param name="userInterfaceScheduler">The UI scheduler.</param>
    /// <param name="requestRepeater">Optional request repeater.</param>
    /// <param name="diffPool">Optional shared diff pool.</param>
    /// <param name="clipboardService">Optional clipboard service used by the copy commands.</param>
    /// <param name="coordinator">
    ///     Optional shared coordinator. When <see langword="null" /> the
    ///     traffic list runs in isolation (used by tests that do not
    ///     exercise cross-panel coordination).
    /// </param>
    public TrafficListViewModel(
        IDomainEventBus eventBus,
        IUserInterfaceScheduler userInterfaceScheduler,
        IRequestRepeater? requestRepeater,
        TrafficFlowDiffPool? diffPool,
        Proxyfan.Presentation.Clipboard.IClipboardService? clipboardService,
        TrafficListCoordinator? coordinator)
    {
        _userInterfaceScheduler = userInterfaceScheduler;
        _requestRepeater = requestRepeater;
        _diffPool = diffPool;
        _clipboardService = clipboardService;
        var effectiveCoordinator = coordinator;
        if (effectiveCoordinator is null)
        {
            var freshCoordinator = new TrafficListCoordinator();
            effectiveCoordinator = freshCoordinator;
        }

        _coordinator = effectiveCoordinator;

        var flowById = new ConcurrentDictionary<Guid, TrafficFlowViewModel>();
        _flowById = flowById;

        var flows = new ObservableCollection<TrafficFlowViewModel>();
        Flows = flows;

        var visibleFlows = new ObservableCollection<TrafficFlowViewModel>();
        VisibleFlows = visibleFlows;

        _filterText = string.Empty;
        _hostFilter = string.Empty;
        _isCapturing = true;

        Flows.CollectionChanged += OnFlowsCollectionChanged;
        _coordinator.HostFilterRequested += OnCoordinatorHostFilterRequested;

        _requestReceivedSubscription = eventBus.Subscribe<RequestReceived>(OnRequestReceived);
        _responseReceivedSubscription = eventBus.Subscribe<ResponseReceived>(OnResponseReceived);
        _flowCompletedSubscription = eventBus.Subscribe<TrafficFlowCompleted>(OnFlowCompleted);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Flows.CollectionChanged -= OnFlowsCollectionChanged;
        _coordinator.HostFilterRequested -= OnCoordinatorHostFilterRequested;
        _requestReceivedSubscription.Dispose();
        _responseReceivedSubscription.Dispose();
        _flowCompletedSubscription.Dispose();
    }

    /// <summary>
    ///     Returns whether the supplied flow matches the current filter text. Empty filter
    ///     matches everything; otherwise checks host, method, path, and status code (case
    ///     insensitive) for substring match.
    /// </summary>
    /// <param name="flow">The flow to evaluate.</param>
    /// <returns>True when the flow should be shown.</returns>
    public bool HasFilterMatch(TrafficFlowViewModel flow)
    {
        if (!HasHostFilterMatch(flow))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        var needle = FilterText;
        var comparison = StringComparison.OrdinalIgnoreCase;

        if (flow.Host.Contains(needle, comparison))
        {
            return true;
        }

        if (flow.Method.Contains(needle, comparison))
        {
            return true;
        }

        if (flow.PathAndQuery.Contains(needle, comparison))
        {
            return true;
        }

        if (flow.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(needle, comparison))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Replaces the current set of captured flows with the supplied imported flows.
    ///     Existing capture state is preserved; flow numbering restarts at 1.
    /// </summary>
    /// <param name="importedFlows">The flows to load into the view model.</param>
    public void LoadFlows(IReadOnlyList<TrafficFlow> importedFlows)
    {
        _userInterfaceScheduler.Post(() => LoadFlowsOnUiThread(importedFlows));
    }

    /// <summary>
    ///     Rebuilds the <see cref="VisibleFlows" /> collection from the current <see cref="Flows" />
    ///     using the active <see cref="FilterText" />.
    /// </summary>
    public void RebuildVisibleFlows()
    {
        _userInterfaceScheduler.Post(RebuildVisibleFlowsOnUiThread);
    }

    [RelayCommand]
    private void AddSelectedToDiffPool()
    {
        if (_diffPool is null)
        {
            return;
        }

        var flow = SelectedFlow;
        if (flow is null)
        {
            return;
        }

        _diffPool.Add(flow.Source);
    }

    [RelayCommand]
    private void ApplyColorTagToSelected(TrafficFlowColorTag colorTag)
    {
        var flow = SelectedFlow;
        if (flow is null)
        {
            return;
        }

        flow.ApplyColorTag(colorTag);
    }

    [RelayCommand]
    private void ApplyCommentToSelected(string? comment)
    {
        var flow = SelectedFlow;
        if (flow is null)
        {
            return;
        }

        flow.ApplyComment(comment);
    }

    [RelayCommand]
    private void Clear()
    {
        _userInterfaceScheduler.Post(ClearOnUiThread);
    }

    private void ClearOnUiThread()
    {
        _flowById.Clear();
        Flows.Clear();
        SelectedFlow = null;
        Interlocked.Exchange(ref _nextNumber, 0);
        _coordinator.NotifyFlowsCleared();
    }

    [RelayCommand]
    private async Task CopySelectedAsCurlAsync(CancellationToken cancellationToken)
    {
        var request = SelectedFlow?.Source?.Request;
        if (request is null || _clipboardService is null)
        {
            return;
        }

        var curl = CurlCommandConverter.ToCurl(request);
        await _clipboardService.SetTextAsync(curl, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CopySelectedAsRawHypertextTransferProtocolAsync(CancellationToken cancellationToken)
    {
        var request = SelectedFlow?.Source?.Request;
        if (request is null || _clipboardService is null)
        {
            return;
        }

        var raw = RawHypertextTransferProtocolMessageFormatter.FormatRequest(request);
        await _clipboardService.SetTextAsync(raw, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CopySelectedUrlAsync(CancellationToken cancellationToken)
    {
        var request = SelectedFlow?.Source?.Request;
        if (request is null || _clipboardService is null)
        {
            return;
        }

        var url = request.RequestUri.ToString();
        await _clipboardService.SetTextAsync(url, cancellationToken).ConfigureAwait(false);
    }

    private bool HasHostFilterMatch(TrafficFlowViewModel flow)
    {
        if (string.IsNullOrWhiteSpace(HostFilter))
        {
            return true;
        }

        return string.Equals(flow.Host, HostFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadFlowsOnUiThread(IReadOnlyList<TrafficFlow> importedFlows)
    {
        ClearOnUiThread();

        var number = 0;
        foreach (var flow in importedFlows)
        {
            number++;
            Interlocked.Exchange(ref _nextNumber, number);
            var viewModel = new TrafficFlowViewModel(flow, number);
            _flowById.TryAdd(flow.Id, viewModel);
            Flows.Add(viewModel);
        }
    }

    private void OnCoordinatorHostFilterRequested(string host)
    {
        _userInterfaceScheduler.Post(() => HostFilter = host);
    }

    partial void OnFilterTextChanged(string value)
    {
        RebuildVisibleFlows();
    }

    private void OnFlowCompleted(TrafficFlowCompleted domainEvent)
    {
        if (!_flowById.TryGetValue(domainEvent.TrafficFlowId, out var viewModel))
        {
            return;
        }

        _userInterfaceScheduler.Post(() => viewModel.UpdateStatus(domainEvent));
    }

    private void OnFlowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs notifyArgs)
    {
        RebuildVisibleFlowsOnUiThread();
    }

    partial void OnHostFilterChanged(string value)
    {
        RebuildVisibleFlows();
    }

    private void OnRequestReceived(RequestReceived domainEvent)
    {
        if (!IsCapturing)
        {
            return;
        }

        var number = Interlocked.Increment(ref _nextNumber);
        var viewModel = new TrafficFlowViewModel(domainEvent, number);
        _flowById.TryAdd(domainEvent.TrafficFlowId, viewModel);

        _userInterfaceScheduler.Post(() => Flows.Add(viewModel));
    }

    private void OnResponseReceived(ResponseReceived domainEvent)
    {
        if (!_flowById.TryGetValue(domainEvent.TrafficFlowId, out var viewModel))
        {
            return;
        }

        _userInterfaceScheduler.Post(() =>
        {
            viewModel.UpdateResponse(domainEvent);
            ReevaluateFlowVisibilityOnUiThread(viewModel);
        });
    }

    private void RebuildVisibleFlowsOnUiThread()
    {
        VisibleFlows.Clear();

        foreach (var flow in Flows)
        {
            if (HasFilterMatch(flow))
            {
                VisibleFlows.Add(flow);
            }
        }
    }

    private void ReevaluateFlowVisibilityOnUiThread(TrafficFlowViewModel viewModel)
    {
        var shouldBeVisible = HasFilterMatch(viewModel);
        var isCurrentlyVisible = VisibleFlows.Contains(viewModel);

        if (shouldBeVisible && !isCurrentlyVisible)
        {
            var targetIndex = Flows.IndexOf(viewModel);
            var insertAt = 0;
            for (var visibleIndex = 0; visibleIndex < VisibleFlows.Count; visibleIndex++)
            {
                if (Flows.IndexOf(VisibleFlows[visibleIndex]) < targetIndex)
                {
                    insertAt = visibleIndex + 1;
                }
            }

            VisibleFlows.Insert(insertAt, viewModel);
        }
        else if (!shouldBeVisible && isCurrentlyVisible)
        {
            VisibleFlows.Remove(viewModel);
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var flow = SelectedFlow;
        if (flow is null)
        {
            return;
        }

        _userInterfaceScheduler.Post(() => RemoveSelectedOnUiThread(flow));
    }

    private void RemoveSelectedOnUiThread(TrafficFlowViewModel viewModel)
    {
        _flowById.TryRemove(viewModel.Source.Id, out _);
        Flows.Remove(viewModel);
        if (ReferenceEquals(SelectedFlow, viewModel))
        {
            SelectedFlow = null;
        }
    }

    private async Task RepeatFlowAsync(
        TrafficFlowViewModel? flow,
        int repeatCount,
        CancellationToken cancellationToken)
    {
        if (_requestRepeater is null)
        {
            return;
        }

        if (flow is null)
        {
            return;
        }

        var request = flow.Request;
        if (request is null)
        {
            return;
        }

        if (string.Equals(request.Method, "CONNECT", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (repeatCount == 1)
        {
            await _requestRepeater.RepeatAsync(request, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _requestRepeater.RepeatAsync(request, repeatCount, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task RepeatSelectedAsync(CancellationToken cancellationToken)
    {
        await RepeatFlowAsync(SelectedFlow, repeatCount: 1, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task RepeatSelectedTenTimesAsync(CancellationToken cancellationToken)
    {
        await RepeatFlowAsync(SelectedFlow, repeatCount: 10, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void ToggleCapture()
    {
        IsCapturing = !IsCapturing;
    }
}
