using CommunityToolkit.Mvvm.ComponentModel;
using Proxyfan.Client.Traffic.ViewModels;
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
    private string _requestBodyText;
    [ObservableProperty]
    private string _requestHeadersText;
    [ObservableProperty]
    private string _responseBodyText;
    [ObservableProperty]
    private string _responseHeadersText;

    /// <summary>
    ///     Initializes a new <see cref="InspectorViewModel" /> and subscribes to selection changes.
    /// </summary>
    /// <param name="trafficListViewModel">
    ///     The traffic list view model whose selected flow is observed.
    /// </param>
    public InspectorViewModel(TrafficListViewModel trafficListViewModel)
    {
        _trafficListViewModel = trafficListViewModel;
        _requestBodyText = string.Empty;
        _requestHeadersText = string.Empty;
        _responseBodyText = string.Empty;
        _responseHeadersText = string.Empty;
        trafficListViewModel.PropertyChanged += OnTrafficListPropertyChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _trafficListViewModel.PropertyChanged -= OnTrafficListPropertyChanged;
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
            RequestHeadersText = string.Empty;
            RequestBodyText = string.Empty;
            ResponseHeadersText = string.Empty;
            ResponseBodyText = string.Empty;
            return;
        }

        if (selectedFlow.Request is not null)
        {
            RequestHeadersText = InspectorTextFormatter.FormatHeaders(selectedFlow.Request.Headers);
            RequestBodyText = InspectorTextFormatter.FormatBody(selectedFlow.Request.Body);
        }
        else
        {
            RequestHeadersText = string.Empty;
            RequestBodyText = string.Empty;
        }

        if (selectedFlow.Response is not null)
        {
            ResponseHeadersText = InspectorTextFormatter.FormatHeaders(selectedFlow.Response.Headers);
            ResponseBodyText = InspectorTextFormatter.FormatBody(selectedFlow.Response.Body);
        }
        else
        {
            ResponseHeadersText = string.Empty;
            ResponseBodyText = string.Empty;
        }
    }
}