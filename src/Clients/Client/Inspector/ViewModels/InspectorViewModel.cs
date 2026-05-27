using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.ComponentModel;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model for the traffic inspector panel. Formats request and response data
///     from the currently selected flow for display.
/// </summary>
public sealed partial class InspectorViewModel : ObservableObject, IDisposable
{
    private readonly TrafficListViewModel _trafficListViewModel;
    [ObservableProperty]
    private string _queryParametersText;
    [ObservableProperty]
    private string _rawRequestText;
    [ObservableProperty]
    private string _rawResponseText;
    [ObservableProperty]
    private string _requestBodyText;
    [ObservableProperty]
    private string _requestCookiesText;
    [ObservableProperty]
    private string _requestHeadersText;
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

    /// <summary>
    ///     Initializes a new <see cref="InspectorViewModel" /> and subscribes to selection changes.
    /// </summary>
    /// <param name="trafficListViewModel">
    ///     The traffic list view model whose selected flow is observed.
    /// </param>
    public InspectorViewModel(TrafficListViewModel trafficListViewModel)
    {
        _trafficListViewModel = trafficListViewModel;
        _queryParametersText = string.Empty;
        _rawRequestText = string.Empty;
        _rawResponseText = string.Empty;
        _requestBodyText = string.Empty;
        _requestCookiesText = string.Empty;
        _requestHeadersText = string.Empty;
        _responseBodyText = string.Empty;
        _responseCookiesText = string.Empty;
        _responseHeadersText = string.Empty;
        _summaryText = string.Empty;
        _timingText = string.Empty;
        trafficListViewModel.PropertyChanged += OnTrafficListPropertyChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _trafficListViewModel.PropertyChanged -= OnTrafficListPropertyChanged;
    }

    private void ApplyRequest(HypertextTransferProtocolRequestData request)
    {
        RequestHeadersText = InspectorTextFormatter.FormatHeaders(request.Headers);
        RequestBodyText = InspectorTextFormatter.FormatBody(request.Body, request.Headers);
        QueryParametersText = QueryStringFormatter.Format(QueryStringParser.Parse(request.RequestUri));
        RequestCookiesText = InspectorCookieFormatter.FormatRequest(request);
        RawRequestText = RawHypertextTransferProtocolMessageFormatter.FormatRequest(request);
    }

    private void ApplyResponse(HypertextTransferProtocolResponseData response)
    {
        ResponseHeadersText = InspectorTextFormatter.FormatHeaders(response.Headers);
        ResponseBodyText = InspectorTextFormatter.FormatBody(response.Body, response.Headers);
        ResponseCookiesText = InspectorCookieFormatter.FormatResponse(response);
        RawResponseText = RawHypertextTransferProtocolMessageFormatter.FormatResponse(response);
    }

    private void ClearAll()
    {
        QueryParametersText = string.Empty;
        RawRequestText = string.Empty;
        RawResponseText = string.Empty;
        RequestHeadersText = string.Empty;
        RequestBodyText = string.Empty;
        RequestCookiesText = string.Empty;
        ResponseHeadersText = string.Empty;
        ResponseBodyText = string.Empty;
        ResponseCookiesText = string.Empty;
        SummaryText = string.Empty;
        TimingText = string.Empty;
    }

    private void ClearRequestSections()
    {
        QueryParametersText = string.Empty;
        RawRequestText = string.Empty;
        RequestHeadersText = string.Empty;
        RequestBodyText = string.Empty;
        RequestCookiesText = string.Empty;
    }

    private void ClearResponseSections()
    {
        RawResponseText = string.Empty;
        ResponseHeadersText = string.Empty;
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

        if (selectedFlow.Source is not null)
        {
            SummaryText = FlowSummaryFormatter.Format(selectedFlow.Source);
            TimingText = FlowTimingFormatter.Format(selectedFlow.Source.Timings);
        }
        else
        {
            SummaryText = string.Empty;
            TimingText = string.Empty;
        }
    }
}