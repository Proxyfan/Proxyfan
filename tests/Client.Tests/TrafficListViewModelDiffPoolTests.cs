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
///     Tests for the <c>Add to Diff Pool</c> context-menu command on
///     <see cref="TrafficListViewModel" />. The command must forward the selected
///     flow's underlying domain source into the shared <see cref="TrafficFlowDiffPool" />,
///     and must remain a safe no-op when the optional pool dependency is absent.
/// </summary>
public sealed class TrafficListViewModelDiffPoolTests
{
    /// <summary>
    ///     With a selected flow, the command appends the underlying flow into the pool.
    /// </summary>
    [Test]
    public async Task AddSelectedToDiffPoolCommand_WithSelectedFlow_AppendsToPool()
    {
        var bus = new RecordingBus();
        var pool = new TrafficFlowDiffPool();
        using var viewModel = new TrafficListViewModel(
            bus,
            InlineUserInterfaceScheduler.Instance,
            requestRepeater: null,
            diffPool: pool);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid()));
        viewModel.SelectedFlow = viewModel.Flows[0];

        viewModel.AddSelectedToDiffPoolCommand.Execute(null);

        await Assert.That(pool.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     With no selection, the command is a no-op (the pool stays empty).
    /// </summary>
    [Test]
    public async Task AddSelectedToDiffPoolCommand_WithoutSelection_DoesNothing()
    {
        var bus = new RecordingBus();
        var pool = new TrafficFlowDiffPool();
        using var viewModel = new TrafficListViewModel(
            bus,
            InlineUserInterfaceScheduler.Instance,
            requestRepeater: null,
            diffPool: pool);

        viewModel.AddSelectedToDiffPoolCommand.Execute(null);

        await Assert.That(pool.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When the optional pool dependency is absent, the command must not throw.
    /// </summary>
    [Test]
    public async Task AddSelectedToDiffPoolCommand_WithoutPool_DoesNotThrow()
    {
        var bus = new RecordingBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid()));
        viewModel.SelectedFlow = viewModel.Flows[0];

        await Assert.That(() => viewModel.AddSelectedToDiffPoolCommand.Execute(null)).ThrowsNothing();
    }

    private static RequestReceived CreateRequestEvent(Guid flowId)
    {
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("https://example.com/api"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }

    private sealed class RecordingBus : IDomainEventBus
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
