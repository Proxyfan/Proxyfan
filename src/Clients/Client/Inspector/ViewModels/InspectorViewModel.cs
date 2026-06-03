using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model for the traffic inspector panel. Formats request and response data
///     from the currently selected flow for display.
/// </summary>
public sealed partial class InspectorViewModel : ObservableObject, IDisposable
{
    private readonly ObservableCollection<TimingPhaseViewModel> _timingPhases;
    private readonly TrafficListViewModel _trafficListViewModel;
    [ObservableProperty]
    private string _authorizationText;
    [ObservableProperty]
    private string _graphQueryLanguageText;
    [ObservableProperty]
    private string _queryParametersText;
    [ObservableProperty]
    private string _rawRequestText;
    [ObservableProperty]
    private string _rawResponseText;
    [ObservableProperty]
    private byte[]? _requestBodyImageBytes;
    [ObservableProperty]
    private string _requestBodyText;
    [ObservableProperty]
    private string _requestCookiesText;
    [ObservableProperty]
    private string _requestHeadersText;
    [ObservableProperty]
    private byte[]? _responseBodyImageBytes;
    [ObservableProperty]
    private string _responseBodyText;
    [ObservableProperty]
    private string _responseCookiesText;
    [ObservableProperty]
    private string _responseHeadersText;
    [ObservableProperty]
    private string _summaryText;
    [ObservableProperty]
    private string _timingText;
    [ObservableProperty]
    private string _totalDurationText;

    /// <summary>
    ///     Gets the child view model that surfaces Remote Procedure Call (gRPC) inspection.
    /// </summary>
    public RemoteProcedureCallInspectorViewModel RemoteProcedureCall { get; }

    /// <summary>
    ///     Gets the child view model that surfaces Server-Sent Events (SSE) inspection.
    /// </summary>
    public ServerSentEventsInspectorViewModel ServerSentEvents { get; }

    /// <summary>
    ///     Gets the phases of the currently selected flow's waterfall, mapped onto a fixed-width lane.
    /// </summary>
    public ReadOnlyObservableCollection<TimingPhaseViewModel> TimingPhases { get; }

    /// <summary>
    ///     Gets the child view model that surfaces WebSocket message inspection.
    /// </summary>
    public WebSocketInspectorViewModel WebSocket { get; }

    /// <summary>
    ///     Initializes a new <see cref="InspectorViewModel" /> and subscribes to selection changes.
    /// </summary>
    /// <param name="trafficListViewModel">
    ///     The traffic list view model whose selected flow is observed.
    /// </param>
    /// <param name="webSocketInspectorViewModel">
    ///     The child WebSocket inspector view model surfaced through the
    ///     <see cref="WebSocket" /> property.
    /// </param>
    /// <param name="serverSentEventsInspectorViewModel">
    ///     The child SSE inspector view model surfaced through the
    ///     <see cref="ServerSentEvents" /> property.
    /// </param>
    /// <param name="remoteProcedureCallInspectorViewModel">
    ///     The child gRPC inspector view model surfaced through the
    ///     <see cref="RemoteProcedureCall" /> property.
    /// </param>
    public InspectorViewModel(
        TrafficListViewModel trafficListViewModel,
        WebSocketInspectorViewModel webSocketInspectorViewModel,
        ServerSentEventsInspectorViewModel serverSentEventsInspectorViewModel,
        RemoteProcedureCallInspectorViewModel remoteProcedureCallInspectorViewModel)
    {
        _trafficListViewModel = trafficListViewModel;
        _authorizationText = string.Empty;
        _graphQueryLanguageText = string.Empty;
        _queryParametersText = string.Empty;
        _rawRequestText = string.Empty;
        _rawResponseText = string.Empty;
        _requestBodyImageBytes = null;
        _requestBodyText = string.Empty;
        _requestCookiesText = string.Empty;
        _requestHeadersText = string.Empty;
        _responseBodyImageBytes = null;
        _responseBodyText = string.Empty;
        _responseCookiesText = string.Empty;
        _responseHeadersText = string.Empty;
        _summaryText = string.Empty;
        _timingText = string.Empty;
        _totalDurationText = string.Empty;
        var phaseCollection = new ObservableCollection<TimingPhaseViewModel>();
        _timingPhases = phaseCollection;
        var readOnlyPhases = new ReadOnlyObservableCollection<TimingPhaseViewModel>(_timingPhases);
        TimingPhases = readOnlyPhases;
        WebSocket = webSocketInspectorViewModel;
        ServerSentEvents = serverSentEventsInspectorViewModel;
        RemoteProcedureCall = remoteProcedureCallInspectorViewModel;
        trafficListViewModel.PropertyChanged += OnTrafficListPropertyChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _trafficListViewModel.PropertyChanged -= OnTrafficListPropertyChanged;
        WebSocket.Dispose();
        ServerSentEvents.Dispose();
        RemoteProcedureCall.Dispose();
    }

    private void ApplyRequest(HypertextTransferProtocolRequestData request)
    {
        RequestHeadersText = InspectorTextFormatter.FormatHeaders(request.Headers);
        RequestBodyText = InspectorTextFormatter.FormatBody(request.Body, request.Headers);
        RequestBodyImageBytes = InspectorImageExtractor.TryExtract(request.Body, request.Headers);
        QueryParametersText = QueryStringFormatter.Format(QueryStringParser.Parse(request.RequestUri));
        RequestCookiesText = InspectorCookieFormatter.FormatRequest(request);
        RawRequestText = RawHypertextTransferProtocolMessageFormatter.FormatRequest(request);
        GraphQueryLanguageText = GraphQueryLanguageInspectorFormatter.Format(request);
        AuthorizationText = AuthorizationInspectorFormatter.Format(request);
    }

    private void ApplyResponse(HypertextTransferProtocolResponseData response)
    {
        ResponseHeadersText = InspectorTextFormatter.FormatHeaders(response.Headers);
        ResponseBodyText = InspectorTextFormatter.FormatBody(response.Body, response.Headers);
        ResponseBodyImageBytes = InspectorImageExtractor.TryExtract(response.Body, response.Headers);
        ResponseCookiesText = InspectorCookieFormatter.FormatResponse(response);
        RawResponseText = RawHypertextTransferProtocolMessageFormatter.FormatResponse(response);
    }

    private void ClearAll()
    {
        AuthorizationText = string.Empty;
        GraphQueryLanguageText = string.Empty;
        QueryParametersText = string.Empty;
        RawRequestText = string.Empty;
        RawResponseText = string.Empty;
        RequestHeadersText = string.Empty;
        RequestBodyImageBytes = null;
        RequestBodyText = string.Empty;
        RequestCookiesText = string.Empty;
        ResponseHeadersText = string.Empty;
        ResponseBodyImageBytes = null;
        ResponseBodyText = string.Empty;
        ResponseCookiesText = string.Empty;
        SummaryText = string.Empty;
        TimingText = string.Empty;
        TotalDurationText = string.Empty;
        _timingPhases.Clear();
    }

    private void ClearRequestSections()
    {
        AuthorizationText = string.Empty;
        GraphQueryLanguageText = string.Empty;
        QueryParametersText = string.Empty;
        RawRequestText = string.Empty;
        RequestHeadersText = string.Empty;
        RequestBodyImageBytes = null;
        RequestBodyText = string.Empty;
        RequestCookiesText = string.Empty;
    }

    private void ClearResponseSections()
    {
        RawResponseText = string.Empty;
        ResponseHeadersText = string.Empty;
        ResponseBodyImageBytes = null;
        ResponseBodyText = string.Empty;
        ResponseCookiesText = string.Empty;
    }

    private void OnTrafficListPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName == nameof(TrafficListViewModel.SelectedFlow))
        {
            UpdateDisplayedText();
        }
    }

    private void UpdateDisplayedText()
    {
        var selectedFlow = _trafficListViewModel.SelectedFlow;

        if (selectedFlow is null)
        {
            ClearAll();
            return;
        }

        if (selectedFlow.Request is not null)
        {
            ApplyRequest(selectedFlow.Request);
        }
        else
        {
            ClearRequestSections();
        }

        if (selectedFlow.Response is not null)
        {
            ApplyResponse(selectedFlow.Response);
        }
        else
        {
            ClearResponseSections();
        }

        var domainFlow = _trafficListViewModel.GetDomainFlow(selectedFlow.Id);
        SummaryText = domainFlow is not null ? FlowSummaryFormatter.Format(domainFlow) : string.Empty;
        var timings = selectedFlow.Timings;
        TimingText = timings is not null ? FlowTimingFormatter.Format(timings) : string.Empty;
        UpdateTimingWaterfall(timings);
    }

    private void UpdateTimingWaterfall(FlowTimings? timings)
    {
        _timingPhases.Clear();
        var phases = TimingWaterfallCalculator.Calculate(timings);

        foreach (var phase in phases)
        {
            var phaseViewModel = new TimingPhaseViewModel(phase);
            _timingPhases.Add(phaseViewModel);
        }

        if (timings is not null && timings.TotalDuration.HasValue)
        {
            TotalDurationText = timings.TotalDuration.Value.TotalMilliseconds
                .ToString("F2", CultureInfo.InvariantCulture) + " ms";
        }
        else
        {
            TotalDurationText = string.Empty;
        }
    }
}