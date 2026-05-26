using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TrafficListViewModel" />.
/// </summary>
public sealed class TrafficListViewModelTests
{
    private sealed class StubDomainEventBus : IDomainEventBus
    {
        public DomainEventHandler<RequestReceived>? RequestReceivedHandler { get; private set; }

        public DomainEventHandler<ResponseReceived>? ResponseReceivedHandler { get; private set; }

        public DomainEventHandler<TrafficFlowCompleted>? FlowCompletedHandler { get; private set; }

        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (typeof(TEvent) == typeof(RequestReceived))
            {
                if (handler is DomainEventHandler<RequestReceived> requestHandler)
                {
                    RequestReceivedHandler = requestHandler;
                }
            }
            else if (typeof(TEvent) == typeof(ResponseReceived))
            {
                if (handler is DomainEventHandler<ResponseReceived> responseHandler)
                {
                    ResponseReceivedHandler = responseHandler;
                }
            }
            else if (typeof(TEvent) == typeof(TrafficFlowCompleted))
            {
                if (handler is DomainEventHandler<TrafficFlowCompleted> completedHandler)
                {
                    FlowCompletedHandler = completedHandler;
                }
            }

            return new StubSubscription();
        }

        private sealed class StubSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficListViewModel.Flows" /> is initially empty.
    /// </summary>
    [Test]
    public async Task Flows_WhenInitialized_IsEmpty()
    {
        var bus = new StubDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficListViewModel.SelectedFlow" /> is initially null.
    /// </summary>
    [Test]
    public async Task SelectedFlow_WhenInitialized_IsNull()
    {
        var bus = new StubDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus);

        await Assert.That(viewModel.SelectedFlow).IsNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficListViewModel.SelectedFlow" /> can be set and retrieved.
    /// </summary>
    [Test]
    public async Task SelectedFlow_WhenSet_ReturnsSetValue()
    {
        var bus = new StubDomainEventBus();
        using var viewModel = new TrafficListViewModel(bus);
        var requestEvent = CreateRequestEvent();
        var flowViewModel = new TrafficFlowViewModel(requestEvent, 1);

        viewModel.SelectedFlow = flowViewModel;

        await Assert.That(viewModel.SelectedFlow).IsSameReferenceAs(flowViewModel);
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficListViewModel" /> subscribes to domain events on construction.
    /// </summary>
    [Test]
    public async Task Constructor_WhenCreated_SubscribesToDomainEvents()
    {
        var bus = new StubDomainEventBus();

        using var viewModel = new TrafficListViewModel(bus);

        await Assert.That(bus.RequestReceivedHandler).IsNotNull();
        await Assert.That(bus.ResponseReceivedHandler).IsNotNull();
        await Assert.That(bus.FlowCompletedHandler).IsNotNull();
    }

    /// <summary>
    ///     Verifies that <see cref="TrafficListViewModel.Dispose" /> does not throw.
    /// </summary>
    [Test]
    public async Task Dispose_WhenCalled_DoesNotThrow()
    {
        var bus = new StubDomainEventBus();
        var viewModel = new TrafficListViewModel(bus);

        await Assert.That(() => viewModel.Dispose()).ThrowsNothing();
    }

    private static RequestReceived CreateRequestEvent()
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri("https://example.com/api");
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }
}