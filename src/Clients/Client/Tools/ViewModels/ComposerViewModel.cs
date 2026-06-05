using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the Request Composer tool. Lets the user assemble a request
///     (method, URL, headers, body), send it directly to the server (bypassing the
///     proxy listener), and manage a starred history of past composed requests.
///     Mirrors the Composer feature in Charles and Fiddler.
/// </summary>
public sealed partial class ComposerViewModel : ObservableObject
{
    private readonly ComposerHistoryService _history;
    private readonly IComposerRequestSender _sender;
    [ObservableProperty]
    private string _body;
    [ObservableProperty]
    private string _curlExport;
    [ObservableProperty]
    private string _headersText;
    [ObservableProperty]
    private string _method;
    [ObservableProperty]
    private string _responseBody;
    [ObservableProperty]
    private string _searchText;
    [ObservableProperty]
    private ComposerHistoryEntryViewModel? _selectedHistoryEntry;
    [ObservableProperty]
    private string _statusText;
    [ObservableProperty]
    private string _url;

    /// <summary>
    ///     Gets the history entries visible in the sidebar, filtered by
    ///     <see cref="SearchText" />.
    /// </summary>
    public ObservableCollection<ComposerHistoryEntryViewModel> HistoryEntries { get; }

    /// <summary>
    ///     Initializes a new <see cref="ComposerViewModel" />.
    /// </summary>
    /// <param name="sender">The sender that dispatches composed requests.</param>
    /// <param name="history">The history service that stores and persists entries.</param>
    public ComposerViewModel(IComposerRequestSender sender, ComposerHistoryService history)
    {
        _sender = sender;
        _history = history;
        _method = "GET";
        _url = string.Empty;
        _headersText = string.Empty;
        _body = string.Empty;
        _statusText = string.Empty;
        _responseBody = string.Empty;
        _curlExport = string.Empty;
        _searchText = string.Empty;
        HistoryEntries = [];
        RefreshHistory();
    }

    /// <summary>
    ///     Builds the current request and returns the captured
    ///     <see cref="HypertextTransferProtocolRequestData" />. Surfaced as a hook for tests
    ///     and the cURL export command.
    /// </summary>
    /// <returns>The composed request, or <see langword="null" /> when the URL is invalid.</returns>
    public HypertextTransferProtocolRequestData? BuildRequest()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return null;
        }

        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var composer = new HypertextTransferProtocolRequestComposer
        {
            Method = string.IsNullOrWhiteSpace(Method) ? "GET" : Method,
            RequestUri = uri,
        };

        var lines = HeadersText.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);

            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            if (name.Length == 0)
            {
                continue;
            }

            composer.SetHeader(name, value);
        }

        if (!string.IsNullOrEmpty(Body))
        {
            composer.Body = Encoding.UTF8.GetBytes(Body);
        }

        var built = composer.Build();
        return built;
    }

    [RelayCommand]
    private void ExportCurl()
    {
        var request = BuildRequest();

        if (request is null)
        {
            CurlExport = string.Empty;
            return;
        }

        CurlExport = CurlCommandConverter.ToCurl(request);
    }

    [RelayCommand]
    private void LoadHistoryEntry()
    {
        if (SelectedHistoryEntry is null)
        {
            return;
        }

        var entry = SelectedHistoryEntry.Source;
        Method = entry.Method;
        Url = entry.Url;
        var builder = new StringBuilder();
        foreach (var header in entry.Headers)
        {
            builder.Append(header.Key);
            builder.Append(": ");
            builder.AppendLine(header.Value);
        }

        HeadersText = builder.ToString();
        Body = Encoding.UTF8.GetString(entry.Body.Span);
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        var matches = _history.Search(SearchText);
        HistoryEntries.Clear();
        foreach (var entry in matches)
        {
            var viewModel = new ComposerHistoryEntryViewModel(entry);
            HistoryEntries.Add(viewModel);
        }
    }

    [RelayCommand]
    private void RemoveHistoryEntry()
    {
        if (SelectedHistoryEntry is null)
        {
            return;
        }

        _history.HasRemoved(SelectedHistoryEntry.Source.Id);
        RefreshHistory();
    }

    [RelayCommand]
    private async Task SendAsync(CancellationToken cancellationToken)
    {
        var request = BuildRequest();

        if (request is null)
        {
            StatusText = "Invalid URL";
            return;
        }

        var sendResult = await _sender.SendAsync(request, cancellationToken).ConfigureAwait(true);
        if (!sendResult.IsSuccess)
        {
            StatusText = sendResult.Error!.Message;
            ResponseBody = string.Empty;
            return;
        }

        var response = sendResult.Value;
        StatusText = response.StatusCode + " " + response.ReasonPhrase;
        ResponseBody = Encoding.UTF8.GetString(response.Body.Span);
        var headerDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (header.Value.Length > 0)
            {
                headerDictionary[header.Key] = header.Value[0];
            }
        }

        var entry = new ComposerHistoryEntry
        {
            Body = request.Body,
            Headers = headerDictionary,
            Id = Guid.NewGuid(),
            IsStarred = false,
            Method = request.Method,
            StatusCode = response.StatusCode,
            Timestamp = DateTimeOffset.UtcNow,
            Url = request.RequestUri.ToString(),
        };
        _history.Add(entry);
        RefreshHistory();
    }

    [RelayCommand]
    private void ToggleStar()
    {
        if (SelectedHistoryEntry is null)
        {
            return;
        }

        _history.HasToggledStar(SelectedHistoryEntry.Source.Id);
        RefreshHistory();
    }
}
