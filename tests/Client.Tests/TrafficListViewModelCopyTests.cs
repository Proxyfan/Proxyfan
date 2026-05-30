using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Diff;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the <c>Copy URL</c>, <c>Copy as cURL</c> and <c>Copy as Raw HTTP</c>
///     commands surfaced by <see cref="TrafficListViewModel" />.
/// </summary>
public sealed class TrafficListViewModelCopyTests
{
    /// <summary>
    ///     Verifies that <c>Copy URL</c> sends the request URI string to the clipboard.
    /// </summary>
    [Test]
    public async Task CopySelectedUrlAsync_WithSelection_CopiesRequestUriToClipboard()
    {
        var bus = new RecordingDomainEventBus();
        var clipboard = new StubClipboardService();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: clipboard);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.CopySelectedUrlCommand.ExecuteAsync(null);

        await Assert.That(clipboard.CopiedTexts.Count).IsEqualTo(1);
        await Assert.That(clipboard.CopiedTexts[0]).IsEqualTo("https://example.com/api");
    }

    /// <summary>
    ///     Verifies that <c>Copy as cURL</c> sends a curl command line to the clipboard.
    /// </summary>
    [Test]
    public async Task CopySelectedAsCurlAsync_WithSelection_CopiesCurlCommandToClipboard()
    {
        var bus = new RecordingDomainEventBus();
        var clipboard = new StubClipboardService();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: clipboard);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "POST"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.CopySelectedAsCurlCommand.ExecuteAsync(null);

        await Assert.That(clipboard.CopiedTexts.Count).IsEqualTo(1);
        await Assert.That(clipboard.CopiedTexts[0]).StartsWith("curl -X POST");
    }

    /// <summary>
    ///     Verifies that <c>Copy as Raw HTTP</c> sends the raw HTTP/1.1 request representation
    ///     to the clipboard.
    /// </summary>
    [Test]
    public async Task CopySelectedAsRawHypertextTransferProtocolAsync_WithSelection_CopiesRawHttpToClipboard()
    {
        var bus = new RecordingDomainEventBus();
        var clipboard = new StubClipboardService();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: clipboard);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.CopySelectedAsRawHypertextTransferProtocolCommand.ExecuteAsync(null);

        await Assert.That(clipboard.CopiedTexts.Count).IsEqualTo(1);
        await Assert.That(clipboard.CopiedTexts[0]).StartsWith("GET https://example.com/api HTTP/1.1");
    }

    /// <summary>
    ///     Verifies that without a selected flow, the copy commands silently no-op.
    /// </summary>
    [Test]
    public async Task CopySelectedUrlAsync_WithNoSelection_DoesNotCopy()
    {
        var bus = new RecordingDomainEventBus();
        var clipboard = new StubClipboardService();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: clipboard);

        await viewModel.CopySelectedUrlCommand.ExecuteAsync(null);

        await Assert.That(clipboard.CopiedTexts.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that without a clipboard service (e.g. tests that don't wire one), the
    ///     commands silently no-op rather than throwing.
    /// </summary>
    [Test]
    public async Task CopySelectedUrlAsync_WithNullClipboardService_DoesNotThrow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await Assert.That(async () => await viewModel.CopySelectedUrlCommand.ExecuteAsync(null)).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that without a clipboard service, <c>Copy as cURL</c> silently no-ops.
    /// </summary>
    [Test]
    public async Task CopySelectedAsCurlAsync_WithNullClipboardService_DoesNotThrow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await Assert.That(async () => await viewModel.CopySelectedAsCurlCommand.ExecuteAsync(null)).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that without a clipboard service, <c>Copy as Raw HTTP</c> silently no-ops.
    /// </summary>
    [Test]
    public async Task CopySelectedAsRawHypertextTransferProtocolAsync_WithNullClipboardService_DoesNotThrow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await Assert.That(async () => await viewModel.CopySelectedAsRawHypertextTransferProtocolCommand.ExecuteAsync(null)).ThrowsNothing();
    }

    private static RequestReceived CreateRequestEvent(Guid flowId, string method)
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = method,
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }

    private sealed class RecordingDomainEventBus : IDomainEventBus
    {
        private readonly List<DomainEventHandler<RequestReceived>> _requestHandlers = [];

        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
            _ = domainEvent;
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (handler is DomainEventHandler<RequestReceived> requestHandler)
            {
                _requestHandlers.Add(requestHandler);
            }

            return new NoOpSubscription();
        }

        public void PublishRequestReceived(RequestReceived domainEvent)
        {
            foreach (var handler in _requestHandlers)
            {
                handler(domainEvent);
            }
        }

        private sealed class NoOpSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
