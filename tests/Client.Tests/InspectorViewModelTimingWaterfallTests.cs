using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
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
        var bus = new StubBus();
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
        var bus = new StubBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        var flow = CreateFlowWithMeasurableTimings();
        var flowViewModel = new TrafficFlowViewModel(flow, 1);

        trafficListViewModel.SelectedFlow = flowViewModel;

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
        var bus = new StubBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        var flow = CreateFlowWithMeasurableTimings();
        var flowViewModel = new TrafficFlowViewModel(flow, 1);
        trafficListViewModel.SelectedFlow = flowViewModel;

        trafficListViewModel.SelectedFlow = null;

        await Assert.That(inspectorViewModel.TimingPhases.Count).IsEqualTo(0);
        await Assert.That(inspectorViewModel.TotalDurationText).IsEqualTo(string.Empty);
    }

    private static TrafficFlow CreateFlowWithMeasurableTimings()
    {
        var requestUri = new Uri("https://example.com/timing");
        var requestHeaders = HeaderCollection.Empty.Add("Host", "example.com");
        var requestParameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = requestHeaders,
            Method = "GET",
            RequestUri = requestUri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(requestParameters);

        var responseHeaders = HeaderCollection.Empty.Add("Content-Type", "text/plain");
        var responseParameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = new byte[] { 104, 105 },
            Headers = responseHeaders,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(responseParameters);

        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9100", DateTimeOffset.UtcNow);
        flow.SetRequest(request);
        Thread.Sleep(20);
        flow.SetResponse(response);
        Thread.Sleep(20);
        flow.Complete();
        return flow;
    }

    private sealed class StubBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            return new StubSubscription();
        }

        private sealed class StubSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
