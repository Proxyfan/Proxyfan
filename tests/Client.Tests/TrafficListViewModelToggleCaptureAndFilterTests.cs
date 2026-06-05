using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Behavioral tests covering the capture-toggle, clear, and filter behaviors of
///     <see cref="TrafficListViewModel" />.
/// </summary>
public sealed class TrafficListViewModelToggleCaptureAndFilterTests
{
    /// <summary>
    ///     The view model must start in the capturing state.
    /// </summary>
    [Test]
    public async Task IsCapturing_Initial_True()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        await Assert.That(viewModel.IsCapturing).IsTrue();
    }

    /// <summary>
    ///     ToggleCapture must flip the capture flag.
    /// </summary>
    [Test]
    public async Task ToggleCapture_FromCapturing_Pauses()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        viewModel.ToggleCaptureCommand.Execute(parameter: null);

        await Assert.That(viewModel.IsCapturing).IsFalse();
    }

    /// <summary>
    ///     ToggleCapture twice restores the capturing flag.
    /// </summary>
    [Test]
    public async Task ToggleCapture_TwoToggles_Resumes()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);

        viewModel.ToggleCaptureCommand.Execute(parameter: null);
        viewModel.ToggleCaptureCommand.Execute(parameter: null);

        await Assert.That(viewModel.IsCapturing).IsTrue();
    }

    /// <summary>
    ///     When capture is paused, RequestReceived events must NOT add new flows.
    /// </summary>
    [Test]
    public async Task OnRequestReceived_WhenPaused_DoesNotAddFlow()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        viewModel.IsCapturing = false;

        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://example.com/api"));

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Clear must remove all flows and reset the next-number counter.
    /// </summary>
    [Test]
    public async Task Clear_WithExistingFlows_EmptiesCollectionAndResetsCounter()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/2"));

        viewModel.ClearCommand.Execute(parameter: null);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
        await Assert.That(viewModel.SelectedFlow).IsNull();

        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/3"));
        await Assert.That(viewModel.Flows[0].Number).IsEqualTo(1);
    }

    /// <summary>
    ///     VisibleFlows must mirror Flows when no filter is set.
    /// </summary>
    [Test]
    public async Task VisibleFlows_NoFilter_MirrorsAllFlows()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "POST", "https://b/2"));

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     VisibleFlows must filter by host substring.
    /// </summary>
    [Test]
    public async Task VisibleFlows_HostFilter_RetainsOnlyMatches()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/x"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://beta.example.com/y"));

        viewModel.FilterText = "alpha";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].Host).IsEqualTo("alpha.example.com");
    }

    /// <summary>
    ///     VisibleFlows must filter by HTTP method substring (case-insensitive).
    /// </summary>
    [Test]
    public async Task VisibleFlows_MethodFilter_RetainsMatches()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "POST", "https://a/2"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "PUT", "https://a/3"));

        viewModel.FilterText = "post";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].Method).IsEqualTo("POST");
    }

    /// <summary>
    ///     VisibleFlows must filter by path substring.
    /// </summary>
    [Test]
    public async Task VisibleFlows_PathFilter_RetainsMatches()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/users"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/orders"));

        viewModel.FilterText = "user";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     VisibleFlows must filter by status code.
    /// </summary>
    [Test]
    public async Task VisibleFlows_StatusCodeFilter_RetainsMatches()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId1 = Guid.NewGuid();
        var flowId2 = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId1, "GET", "https://a/1"));
        bus.PublishRequestReceived(CreateRequestEvent(flowId2, "GET", "https://a/2"));
        bus.PublishResponseReceived(CreateResponseEvent(flowId1, 404));
        bus.PublishResponseReceived(CreateResponseEvent(flowId2, 200));

        viewModel.FilterText = "404";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].StatusCode).IsEqualTo(404);
    }

    /// <summary>
    ///     Status-code filters must be reevaluated when a matching response arrives later.
    /// </summary>
    [Test]
    public async Task VisibleFlows_StatusCodeFilter_ResponseArrival_AddsMatch()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET", "https://a/1"));
        viewModel.FilterText = "404";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(0);

        bus.PublishResponseReceived(CreateResponseEvent(flowId, 404));

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].StatusCode).IsEqualTo(404);
    }

    /// <summary>
    ///     Status-code filters must remove rows whose updated response no longer matches.
    /// </summary>
    [Test]
    public async Task VisibleFlows_StatusCodeFilter_ResponseUpdate_RemovesStaleMatch()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var flowId = Guid.NewGuid();
        bus.PublishRequestReceived(CreateRequestEvent(flowId, "GET", "https://a/1"));
        bus.PublishResponseReceived(CreateResponseEvent(flowId, 404));
        viewModel.FilterText = "404";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);

        bus.PublishResponseReceived(CreateResponseEvent(flowId, 200));

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Appending a non-matching flow while the filter is unchanged must not rebuild the
    ///     visible collection.
    /// </summary>
    [Test]
    public async Task VisibleFlows_FilteredAppendWithNonMatch_DoesNotRaiseCollectionChanged()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/1"));
        viewModel.FilterText = "alpha";

        var actions = new List<NotifyCollectionChangedAction>();
        viewModel.VisibleFlows.CollectionChanged += (_, eventArgs) => actions.Add(eventArgs.Action);

        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://beta.example.com/2"));

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].Host).IsEqualTo("alpha.example.com");
        await Assert.That(actions.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Removing a visible flow while the filter is unchanged must surface a remove
    ///     notification instead of rebuilding the collection.
    /// </summary>
    [Test]
    public async Task VisibleFlows_FilteredRemoveSelected_RaisesSingleRemoveNotification()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/1"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/2"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://beta.example.com/3"));
        viewModel.FilterText = "alpha";
        viewModel.SelectedFlow = viewModel.VisibleFlows[0];

        var actions = new List<NotifyCollectionChangedAction>();
        viewModel.VisibleFlows.CollectionChanged += (_, eventArgs) => actions.Add(eventArgs.Action);

        viewModel.RemoveSelectedCommand.Execute(parameter: null);

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(viewModel.VisibleFlows[0].Host).IsEqualTo("alpha.example.com");
        await Assert.That(actions.Count).IsEqualTo(1);
        await Assert.That(actions[0]).IsEqualTo(NotifyCollectionChangedAction.Remove);
    }

    /// <summary>
    ///     Non-appended insertions must fall back to a rebuild so the filtered visible order
    ///     stays aligned with the source collection.
    /// </summary>
    [Test]
    public async Task VisibleFlows_FilteredInsertAtStart_RebuildsToPreserveOrder()
    {
        const int insertedSequenceNumber = 99;

        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://beta.example.com/2"));
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/3"));
        viewModel.FilterText = "alpha";
        var insertedFlow = new TrafficFlowViewModel(
            CreateRequestEvent(Guid.NewGuid(), "GET", "https://alpha.example.com/1"),
            insertedSequenceNumber);

        var actions = new List<NotifyCollectionChangedAction>();
        viewModel.VisibleFlows.CollectionChanged += (_, eventArgs) => actions.Add(eventArgs.Action);

        viewModel.Flows.Insert(0, insertedFlow);

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(2);
        await Assert.That(viewModel.VisibleFlows[0]).IsSameReferenceAs(insertedFlow);
        await Assert.That(viewModel.VisibleFlows[1].Host).IsEqualTo("alpha.example.com");
        await Assert.That(actions.Count).IsGreaterThan(0);
        await Assert.That(actions[0]).IsEqualTo(NotifyCollectionChangedAction.Reset);
    }

    /// <summary>
    ///     Empty filter text means everything is visible.
    /// </summary>
    [Test]
    public async Task HasFilterMatch_EmptyFilter_AlwaysTrue()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));

        viewModel.FilterText = string.Empty;

        await Assert.That(viewModel.HasFilterMatch(viewModel.Flows[0])).IsTrue();
    }

    /// <summary>
    ///     Whitespace-only filter text matches everything.
    /// </summary>
    [Test]
    public async Task HasFilterMatch_WhitespaceFilter_AlwaysTrue()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));

        viewModel.FilterText = "   ";

        await Assert.That(viewModel.HasFilterMatch(viewModel.Flows[0])).IsTrue();
    }

    /// <summary>
    ///     A non-matching filter must result in no visible flows.
    /// </summary>
    [Test]
    public async Task VisibleFlows_NonMatchingFilter_Empty()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));

        viewModel.FilterText = "nope";

        await Assert.That(viewModel.VisibleFlows.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     LoadFlows replaces the current flow set with the supplied flows.
    /// </summary>
    [Test]
    public async Task LoadFlows_GivenFlows_ReplacesExistingCollection()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));

        var imported = new List<TrafficFlow>
        {
            new(Guid.NewGuid(), "127.0.0.1:9001", DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "127.0.0.1:9002", DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "127.0.0.1:9003", DateTimeOffset.UtcNow),
        };
        viewModel.LoadFlows(imported);

        await Assert.That(viewModel.Flows.Count).IsEqualTo(3);
        await Assert.That(viewModel.Flows[0].Number).IsEqualTo(1);
        await Assert.That(viewModel.Flows[2].Number).IsEqualTo(3);
    }

    /// <summary>
    ///     LoadFlows with an empty list clears the collection.
    /// </summary>
    [Test]
    public async Task LoadFlows_EmptyList_ClearsCollection()
    {
        var bus = new StubBus();
        using var viewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        bus.PublishRequestReceived(CreateRequestEvent(Guid.NewGuid(), "GET", "https://a/1"));

        viewModel.LoadFlows(new List<TrafficFlow>());

        await Assert.That(viewModel.Flows.Count).IsEqualTo(0);
    }

    private static RequestReceived CreateRequestEvent(Guid flowId, string method, string url)
    {
        var uri = new Uri(url);
        var headers = HeaderCollection.Empty.Add("Host", uri.Host);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = method,
            RequestUri = uri,
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

    private sealed class StubBus : IDomainEventBus
    {
        private readonly List<DomainEventHandler<RequestReceived>> _requestHandlers;
        private readonly List<DomainEventHandler<ResponseReceived>> _responseHandlers;
        private readonly List<DomainEventHandler<TrafficFlowCompleted>> _flowCompletedHandlers;

        public StubBus()
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
