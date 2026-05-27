using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ComposerViewModel" /> covering request building, sending, history
///     management (star/remove/load/refresh) and cURL export.
/// </summary>
public sealed class ComposerViewModelTests
{
    /// <summary>
    ///     Verifies that <see cref="ComposerViewModel.BuildRequest" /> returns null when the URL
    ///     is whitespace.
    /// </summary>
    [Test]
    public async Task BuildRequest_BlankUrl_ReturnsNull()
    {
        var viewModel = CreateViewModel();
        viewModel.Url = "   ";

        var request = viewModel.BuildRequest();

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="ComposerViewModel.BuildRequest" /> returns null when the URL
    ///     is not a valid absolute URI.
    /// </summary>
    [Test]
    public async Task BuildRequest_InvalidUrl_ReturnsNull()
    {
        var viewModel = CreateViewModel();
        viewModel.Url = "not a url";

        var request = viewModel.BuildRequest();

        await Assert.That(request).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="ComposerViewModel.BuildRequest" /> populates method, URL,
    ///     headers and body when the inputs are valid.
    /// </summary>
    [Test]
    public async Task BuildRequest_ValidInputs_ReturnsPopulatedRequest()
    {
        var viewModel = CreateViewModel();
        viewModel.Method = "POST";
        viewModel.Url = "https://example.com/api";
        viewModel.HeadersText = "Accept: application/json\nX-Custom: value";
        viewModel.Body = "{}";

        var request = viewModel.BuildRequest();

        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Method).IsEqualTo("POST");
        await Assert.That(request.RequestUri.ToString()).IsEqualTo("https://example.com/api");
        await Assert.That(request.Headers.GetAll("Accept")[0]).IsEqualTo("application/json");
        await Assert.That(request.Headers.GetAll("X-Custom")[0]).IsEqualTo("value");
        await Assert.That(Encoding.UTF8.GetString(request.Body.Span)).IsEqualTo("{}");
    }

    /// <summary>
    ///     Verifies that the cURL export command produces a non-empty command line for a valid
    ///     request.
    /// </summary>
    [Test]
    public async Task ExportCurl_ValidRequest_ProducesCommandLine()
    {
        var viewModel = CreateViewModel();
        viewModel.Url = "https://example.com/";

        viewModel.ExportCurlCommand.Execute(null);

        await Assert.That(viewModel.CurlExport).IsNotEmpty();
        await Assert.That(viewModel.CurlExport).Contains("curl");
    }

    /// <summary>
    ///     Verifies that the cURL export command clears the export when the URL is invalid.
    /// </summary>
    [Test]
    public async Task ExportCurl_InvalidUrl_ClearsExport()
    {
        var viewModel = CreateViewModel();
        viewModel.CurlExport = "previous";
        viewModel.Url = string.Empty;

        viewModel.ExportCurlCommand.Execute(null);

        await Assert.That(viewModel.CurlExport).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that loading a selected history entry restores its method, URL, headers and
    ///     body into the editor.
    /// </summary>
    [Test]
    public async Task LoadHistoryEntry_SelectedEntry_RestoresFields()
    {
        var store = new StubComposerHistoryStore();
        var entry = CreateEntry("PUT", "https://example.org/x");
        store.Entries = [entry];
        var viewModel = CreateViewModel(store: store);
        viewModel.SelectedHistoryEntry = viewModel.HistoryEntries[0];

        viewModel.LoadHistoryEntryCommand.Execute(null);

        await Assert.That(viewModel.Method).IsEqualTo("PUT");
        await Assert.That(viewModel.Url).IsEqualTo("https://example.org/x");
        await Assert.That(viewModel.HeadersText).Contains("Accept: application/json");
        await Assert.That(viewModel.Body).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that loading without a selected entry leaves the editor unchanged.
    /// </summary>
    [Test]
    public async Task LoadHistoryEntry_NoSelection_LeavesEditorUnchanged()
    {
        var viewModel = CreateViewModel();
        viewModel.Method = "PATCH";
        viewModel.Url = "https://untouched.example/";

        viewModel.LoadHistoryEntryCommand.Execute(null);

        await Assert.That(viewModel.Method).IsEqualTo("PATCH");
        await Assert.That(viewModel.Url).IsEqualTo("https://untouched.example/");
    }

    /// <summary>
    ///     Verifies that the search text filters the visible history entries by URL substring.
    /// </summary>
    [Test]
    public async Task SearchText_PartialMatch_FiltersHistoryEntries()
    {
        var store = new StubComposerHistoryStore
        {
            Entries =
            [
                CreateEntry("GET", "https://alpha.example/"),
                CreateEntry("GET", "https://beta.example/"),
            ],
        };
        var viewModel = CreateViewModel(store: store);

        viewModel.SearchText = "beta";

        await Assert.That(viewModel.HistoryEntries.Count).IsEqualTo(1);
        await Assert.That(viewModel.HistoryEntries[0].Url).IsEqualTo("https://beta.example/");
    }

    /// <summary>
    ///     Verifies that sending a request without a valid URL sets a status message and does
    ///     not invoke the sender.
    /// </summary>
    [Test]
    public async Task SendAsync_InvalidUrl_SetsStatusAndDoesNotInvokeSender()
    {
        var sender = new StubComposerRequestSender();
        var viewModel = CreateViewModel(sender: sender);
        viewModel.Url = string.Empty;

        await viewModel.SendCommand.ExecuteAsync(null);

        await Assert.That(viewModel.StatusText).IsEqualTo("Invalid URL");
        await Assert.That(sender.CapturedRequests.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that sending a valid request populates the status, response body and adds
    ///     the entry to history.
    /// </summary>
    [Test]
    public async Task SendAsync_ValidRequest_PopulatesResponseAndAppendsHistory()
    {
        var store = new StubComposerHistoryStore();
        var sender = new StubComposerRequestSender
        {
            ResponseToReturn = new HypertextTransferProtocolResponseData(
                new HypertextTransferProtocolResponseDataParameters
                {
                    Body = Encoding.UTF8.GetBytes("body-text"),
                    Headers = HeaderCollection.Empty,
                    ReasonPhrase = "Created",
                    StatusCode = 201,
                    Version = "HTTP/1.1",
                }),
        };
        var viewModel = CreateViewModel(sender: sender, store: store);
        viewModel.Method = "POST";
        viewModel.Url = "https://example.com/";
        viewModel.HeadersText = "Accept: text/plain";

        await viewModel.SendCommand.ExecuteAsync(null);

        await Assert.That(viewModel.StatusText).IsEqualTo("201 Created");
        await Assert.That(viewModel.ResponseBody).IsEqualTo("body-text");
        await Assert.That(viewModel.HistoryEntries.Count).IsEqualTo(1);
        await Assert.That(viewModel.HistoryEntries[0].Method).IsEqualTo("POST");
        await Assert.That(store.SaveCallCount).IsGreaterThan(0);
    }

    /// <summary>
    ///     Verifies that an <see cref="HttpRequestException" /> thrown by the sender is caught
    ///     and surfaced as the status text.
    /// </summary>
    [Test]
    public async Task SendAsync_SenderThrows_SetsStatusToMessage()
    {
        var sender = new StubComposerRequestSender
        {
            ExceptionToThrow = new HttpRequestException("boom"),
        };
        var viewModel = CreateViewModel(sender: sender);
        viewModel.Url = "https://example.com/";

        await viewModel.SendCommand.ExecuteAsync(null);

        await Assert.That(viewModel.StatusText).IsEqualTo("boom");
        await Assert.That(viewModel.ResponseBody).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that toggling the star flag on the selected entry flips
    ///     <see cref="ComposerHistoryEntry.IsStarred" /> in the store.
    /// </summary>
    [Test]
    public async Task ToggleStar_SelectedEntry_FlipsStarredFlag()
    {
        var entry = CreateEntry("GET", "https://example.com/");
        var store = new StubComposerHistoryStore { Entries = [entry] };
        var viewModel = CreateViewModel(store: store);
        viewModel.SelectedHistoryEntry = viewModel.HistoryEntries[0];

        viewModel.ToggleStarCommand.Execute(null);

        await Assert.That(store.Entries[0].IsStarred).IsTrue();
    }

    /// <summary>
    ///     Verifies that removing the selected entry deletes it from the store and refreshes the
    ///     list.
    /// </summary>
    [Test]
    public async Task RemoveHistoryEntry_SelectedEntry_RemovesFromStoreAndList()
    {
        var entry = CreateEntry("GET", "https://example.com/");
        var store = new StubComposerHistoryStore { Entries = [entry] };
        var viewModel = CreateViewModel(store: store);
        viewModel.SelectedHistoryEntry = viewModel.HistoryEntries[0];

        viewModel.RemoveHistoryEntryCommand.Execute(null);

        await Assert.That(store.Entries.Count).IsEqualTo(0);
        await Assert.That(viewModel.HistoryEntries.Count).IsEqualTo(0);
    }

    private static ComposerHistoryEntry CreateEntry(string method, string url)
    {
        return new ComposerHistoryEntry
        {
            Body = Encoding.UTF8.GetBytes("hello"),
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = "application/json",
            },
            Id = Guid.NewGuid(),
            IsStarred = false,
            Method = method,
            StatusCode = 200,
            Timestamp = DateTimeOffset.UtcNow,
            Url = url,
        };
    }

    private static ComposerViewModel CreateViewModel(
        IComposerRequestSender? sender = null,
        IComposerHistoryStore? store = null)
    {
        var actualSender = sender ?? new StubComposerRequestSender();
        var actualStore = store ?? new StubComposerHistoryStore();
        var history = new ComposerHistoryService(actualStore);
        return new ComposerViewModel(actualSender, history);
    }
}
