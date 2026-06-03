using Proxyfan.Client.Inspector.ViewModels;
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
///     Tests for the timing waterfall integration in <see cref="InspectorViewModel" />.
/// </summary>
public sealed class InspectorViewModelTimingWaterfallTests
{
    /// <summary>
    ///     Verifies that the timing phases collection starts empty when no flow is selected.
    /// </summary>
    [Test]
    public async Task TimingPhases_NoFlowSelected_IsEmpty()
    {
        var bus = new RecordingBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);

        await Assert.That(inspectorViewModel.TimingPhases.Count).IsEqualTo(0);
        await Assert.That(inspectorViewModel.TotalDurationText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that selecting a flow with measurable timings populates the
    ///     waterfall and total duration label.
    /// </summary>
    [Test]
    public async Task TimingPhases_FlowWithMeasurableTimings_PopulatesAndFormats()
    {
        var bus = new RecordingBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        PublishFlowWithMeasurableTimings(bus);

        trafficListViewModel.SelectedFlow = trafficListViewModel.Flows[0];

        await Assert.That(inspectorViewModel.TimingPhases.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(inspectorViewModel.TotalDurationText).IsNotEmpty();
        await Assert.That(inspectorViewModel.TotalDurationText.EndsWith(" ms", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that the waterfall and total duration clear when the flow is deselected.
    /// </summary>
    [Test]
    public async Task TimingPhases_FlowDeselected_ClearsWaterfall()
    {
        var bus = new RecordingBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        PublishFlowWithMeasurableTimings(bus);
        trafficListViewModel.SelectedFlow = trafficListViewModel.Flows[0];

        trafficListViewModel.SelectedFlow = null;

        await Assert.That(inspectorViewModel.TimingPhases.Count).IsEqualTo(0);
        await Assert.That(inspectorViewModel.TotalDurationText).IsEqualTo(string.Empty);
    }

    private static void PublishFlowWithMeasurableTimings(RecordingBus bus)
    {
        var id = Guid.NewGuid();
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("Host", "example.com"),
            Method = "GET",
            RequestUri = new Uri("https://example.com/timing"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);
        bus.PublishRequestReceived(new RequestReceived(id, request, "127.0.0.1:9100", DateTimeOffset.UtcNow));
        Thread.Sleep(20);
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = new byte[] { 104, 105 },
            Headers = HeaderCollection.Empty.Add("Content-Type", "text/plain"),
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);
        bus.PublishResponseReceived(new ResponseReceived(id, response, DateTimeOffset.UtcNow));
        Thread.Sleep(20);
        bus.PublishFlowCompleted(new TrafficFlowCompleted(id, TrafficFlowStatus.Complete, DateTimeOffset.UtcNow));
    }

    private sealed class RecordingBus : IDomainEventBus
    {
        private readonly List<DomainEventHandler<RequestReceived>> _requestHandlers = [];
        private readonly List<DomainEventHandler<ResponseReceived>> _responseHandlers = [];
        private readonly List<DomainEventHandler<TrafficFlowCompleted>> _completedHandlers = [];

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

            if (handler is DomainEventHandler<ResponseReceived> responseHandler)
            {
                _responseHandlers.Add(responseHandler);
            }

            if (handler is DomainEventHandler<TrafficFlowCompleted> completedHandler)
            {
                _completedHandlers.Add(completedHandler);
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

        public void PublishResponseReceived(ResponseReceived domainEvent)
        {
            foreach (var handler in _responseHandlers)
            {
                handler(domainEvent);
            }
        }

        public void PublishFlowCompleted(TrafficFlowCompleted domainEvent)
        {
            foreach (var handler in _completedHandlers)
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

