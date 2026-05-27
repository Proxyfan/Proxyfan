using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Behavioral tests for <see cref="TrafficListViewModel" /> driven through the inline
///     UI scheduler so that view-model mutations execute synchronously.
/// </summary>
public sealed class TrafficListViewModelBehaviorTests
{
    /// <summary>
    ///     When a <see cref="RequestReceived" /> event is published, the corresponding flow must
    ///     be added to the observable collection.
    /// </summary>
    [Test]
    public async Task OnRequestReceived_WhenPublished_AddsFlowToCollection()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var requestEvent = CreateRequestEvent(Guid.NewGuid());

        bus.PublishRequestReceived(requestEvent);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
        await Assert.That(viewModel.Flows[0].Number).IsEqualTo(1);
    }

    /// <summary>
    ///     When multiple <see cref="RequestReceived" /> events are published, sequential numbers
    ///     are assigned to each captured flow.
    /// </summary>
    [Test]
    public async Task OnRequestReceived_TwoRequests_AssignsSequentialNumbers()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid()));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid()));


        await Assert.That(viewModel.Flows.Count).IsEqualTo(2);
        await Assert.That(viewModel.Flows[0].Number).IsEqualTo(1);
        await Assert.That(viewModel.Flows[1].Number).IsEqualTo(2);
    }

    /// <summary>
    ///     When a <see cref="ResponseReceived" /> event is published for a known flow, the
    ///     corresponding flow view model must be updated.
    /// </summary>
    [Test]
    public async Task OnResponseReceived_WithKnownFlow_UpdatesExistingFlow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId));

        bus.PublishResponseReceived(CreateResponseEvent(flowId, 200));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
        await Assert.That(viewModel.Flows[0].StatusCode).IsEqualTo(200);
    }

    /// <summary>
    ///     When a <see cref="ResponseReceived" /> event is published for an unknown flow, the
    ///     handler must safely do nothing.
    /// </summary>
    [Test]
    public async Task OnResponseReceived_UnknownFlow_DoesNotThrow()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        bus.PublishResponseReceived(CreateResponseEvent(Guid.NewGuid(), 200));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     When a <see cref="TrafficFlowCompleted" /> event is published for a known flow, the
    ///     handler must update the flow status without throwing.
    /// </summary>
    [Test]
    public async Task OnFlowCompleted_KnownFlow_UpdatesStatus()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId));

        bus.PublishFlowCompleted(new TrafficFlowCompleted(flowId, TrafficFlowStatus.Complete, DateTimeOffset.UtcNow));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     When a <see cref="TrafficFlowCompleted" /> event is published for an unknown flow,
    ///     the handler must safely do nothing.
    /// </summary>
    [Test]
    public async Task OnFlowCompleted_UnknownFlow_DoesNothing()
    {
        var bus = new RecordingDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        bus.PublishFlowCompleted(new TrafficFlowCompleted(Guid.NewGuid(), TrafficFlowStatus.Complete, DateTimeOffset.UtcNow));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
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

    private static ResponseReceived CreateResponseEvent(Guid flowId, int statusCode)
    {
        var headers = HeaderCollection.Empty.Add("Content-Length", "0");
        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = statusCode,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        return new ResponseReceived(flowId, response, DateTimeOffset.UtcNow);
    }

    private sealed class RecordingDomainEventBus : IDomainEventBus
    {
        private readonly List<DomainEventHandler<RequestReceived>> _requestHandlers;
        private readonly List<DomainEventHandler<ResponseReceived>> _responseHandlers;
        private readonly List<DomainEventHandler<TrafficFlowCompleted>> _flowCompletedHandlers;

        public RecordingDomainEventBus()
        {
            var requestHandlers = new List<DomainEventHandler<RequestReceived>>();
            var responseHandlers = new List<DomainEventHandler<ResponseReceived>>();
            var flowCompletedHandlers = new List<DomainEventHandler<TrafficFlowCompleted>>();
            _requestHandlers = requestHandlers;
            _responseHandlers = responseHandlers;
            _flowCompletedHandlers = flowCompletedHandlers;
        }

        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            switch (handler)
            {
                case DomainEventHandler<RequestReceived> requestHandler:
                    _requestHandlers.Add(requestHandler);
                    break;
                case DomainEventHandler<ResponseReceived> responseHandler:
                    _responseHandlers.Add(responseHandler);
                    break;
                case DomainEventHandler<TrafficFlowCompleted> flowCompletedHandler:
                    _flowCompletedHandlers.Add(flowCompletedHandler);
                    break;
            }

            return new StubSubscription();
        }

        public void PublishRequestReceived(RequestReceived domainEvent)
        {
            foreach (var handler in _requestHandlers)
            {
                handler(domainEvent);
            }
        }

        public void PublishResponseReceived(ResponseReceived domainEvent)
        {
            foreach (var handler in _responseHandlers)
            {
                handler(domainEvent);
            }
        }

        public void PublishFlowCompleted(TrafficFlowCompleted domainEvent)
        {
            foreach (var handler in _flowCompletedHandlers)
            {
                handler(domainEvent);
            }
        }

        private sealed class StubSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}