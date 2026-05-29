using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Additional tests for <see cref="InspectorViewModel" /> covering response display
///     and unrelated property change events.
/// </summary>
public sealed class InspectorViewModelAdditionalTests
{
    /// <summary>
    ///     Verifies that response text is populated when a flow with a response is selected.
    /// </summary>
    [Test]
    public async Task UpdateDisplayedText_WhenFlowWithResponseSelected_ResponseTextIsPopulated()
    {
        var bus = new StubBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        var flow = CreateTrafficFlowWithResponse();
        var flowViewModel = new TrafficFlowViewModel(flow, 1);

        trafficListViewModel.SelectedFlow = flowViewModel;

        await Assert.That(inspectorViewModel.ResponseHeadersText).IsNotEmpty();
        await Assert.That(inspectorViewModel.ResponseBodyText).IsNotEmpty();
    }

    /// <summary>
    ///     Verifies that selecting a flow with no request or response (a tunnel-style flow)
    ///     clears the inspector text sections (covers the false branches of UpdateDisplayedText).
    /// </summary>
    [Test]
    public async Task UpdateDisplayedText_WhenTunnelFlowSelectedAfterPopulatedFlow_ClearsSections()
    {
        var bus = new StubBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        using var inspectorViewModel = InspectorViewModelFactory.Create(trafficListViewModel);
        var populatedFlow = CreateTrafficFlowWithResponse();
        var populatedViewModel = new TrafficFlowViewModel(populatedFlow, 1);
        trafficListViewModel.SelectedFlow = populatedViewModel;
        var tunnelFlow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9002", DateTimeOffset.UtcNow);
        var tunnelViewModel = new TrafficFlowViewModel(tunnelFlow, 2);

        trafficListViewModel.SelectedFlow = tunnelViewModel;

        await Assert.That(inspectorViewModel.RequestHeadersText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.RequestBodyText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.ResponseHeadersText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.ResponseBodyText).IsEqualTo(string.Empty);
    }

    private static TrafficFlow CreateTrafficFlowWithResponse()
    {
        var requestUri = new Uri("https://example.com/api");
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

        var flow = new TrafficFlow(Guid.NewGuid(), "127.0.0.1:9001", DateTimeOffset.UtcNow);
        flow.SetRequest(request);
        flow.SetResponse(response);
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