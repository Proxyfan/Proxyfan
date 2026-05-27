using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the <c>Repeat</c> and <c>Repeat 10 Times</c> commands surfaced by
///     <see cref="TrafficListViewModel" />. The commands must delegate to the supplied
///     <see cref="IRequestRepeater" /> for a selected non-CONNECT flow, and must no-op
///     otherwise.
/// </summary>
public sealed class TrafficListViewModelRepeatTests
{
    /// <summary>
    ///     Verifies that the single-shot repeat command forwards the selected flow's request
    ///     to the repeater exactly once.
    /// </summary>
    [Test]
    public async Task RepeatSelectedAsync_WithSelection_InvokesRepeaterOnce()
    {
        var bus = new RecordingDomainEventBus();
        var repeater = new StubRequestRepeater();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, repeater);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.RepeatSelectedCommand.ExecuteAsync(null);

        await Assert.That(repeater.SingleInvocations.Count).IsEqualTo(1);
        await Assert.That(repeater.MultiInvocations.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the 10x repeat command forwards the request once with the
    ///     correct repeat count.
    /// </summary>
    [Test]
    public async Task RepeatSelectedTenTimesAsync_WithSelection_InvokesRepeaterWithCountTen()
    {
        var bus = new RecordingDomainEventBus();
        var repeater = new StubRequestRepeater();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, repeater);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.RepeatSelectedTenTimesCommand.ExecuteAsync(null);

        await Assert.That(repeater.MultiInvocations.Count).IsEqualTo(1);
        await Assert.That(repeater.MultiInvocations[0].Count).IsEqualTo(10);
    }

    /// <summary>
    ///     Verifies that the command is a no-op when no flow is selected.
    /// </summary>
    [Test]
    public async Task RepeatSelectedAsync_NoSelection_DoesNotInvokeRepeater()
    {
        var bus = new RecordingDomainEventBus();
        var repeater = new StubRequestRepeater();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, repeater);

        await viewModel.RepeatSelectedCommand.ExecuteAsync(null);

        await Assert.That(repeater.SingleInvocations.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the command is a no-op for CONNECT flows (HTTPS tunnels). Repeating a
    ///     tunnel handshake is meaningless.
    /// </summary>
    [Test]
    public async Task RepeatSelectedAsync_ConnectFlow_DoesNotInvokeRepeater()
    {
        var bus = new RecordingDomainEventBus();
        var repeater = new StubRequestRepeater();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, repeater);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "CONNECT"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await viewModel.RepeatSelectedCommand.ExecuteAsync(null);

        await Assert.That(repeater.SingleInvocations.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that the command silently no-ops when constructed without a repeater
    ///     (e.g. design-time or tests that do not need replay).
    /// </summary>
    [Test]
    public async Task RepeatSelectedAsync_WithoutRepeater_DoesNotThrow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET"));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await Assert.That(async () => await viewModel.RepeatSelectedCommand.ExecuteAsync(null)).ThrowsNothing();
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
