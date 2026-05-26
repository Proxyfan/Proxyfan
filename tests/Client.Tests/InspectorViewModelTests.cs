using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="InspectorViewModel" />.
/// </summary>
public sealed class InspectorViewModelTests
{
    /// <summary>
    ///     Verifies that all text properties are empty when no flow is selected.
    /// </summary>
    [Test]
    public async Task UpdateDisplayedText_WhenNoFlowSelected_AllTextIsEmpty()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus);
        using var inspectorViewModel = new InspectorViewModel(trafficListViewModel);

        await Assert.That(inspectorViewModel.RequestHeadersText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.RequestBodyText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.ResponseHeadersText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.ResponseBodyText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that request text is populated when a flow with a request is selected.
    /// </summary>
    [Test]
    public async Task UpdateDisplayedText_WhenFlowWithRequestSelected_RequestTextIsPopulated()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus);
        using var inspectorViewModel = new InspectorViewModel(trafficListViewModel);
        var requestEvent = CreateRequestEvent();
        var flowViewModel = new TrafficFlowViewModel(requestEvent, 1);

        trafficListViewModel.SelectedFlow = flowViewModel;

        await Assert.That(inspectorViewModel.RequestHeadersText).IsNotEmpty();
    }

    /// <summary>
    ///     Verifies that all text is cleared when selected flow is set to null.
    /// </summary>
    [Test]
    public async Task UpdateDisplayedText_WhenFlowDeselected_AllTextIsCleared()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus);
        using var inspectorViewModel = new InspectorViewModel(trafficListViewModel);
        var requestEvent = CreateRequestEvent();
        var flowViewModel = new TrafficFlowViewModel(requestEvent, 1);
        trafficListViewModel.SelectedFlow = flowViewModel;

        trafficListViewModel.SelectedFlow = null;

        await Assert.That(inspectorViewModel.RequestHeadersText).IsEqualTo(string.Empty);
        await Assert.That(inspectorViewModel.RequestBodyText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that disposal unsubscribes from property change events.
    /// </summary>
    [Test]
    public async Task Dispose_WhenDisposed_NoLongerUpdatesOnSelectionChange()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus);
        var inspectorViewModel = new InspectorViewModel(trafficListViewModel);
        var requestEvent = CreateRequestEvent();
        var flowViewModel = new TrafficFlowViewModel(requestEvent, 1);

        inspectorViewModel.Dispose();
        trafficListViewModel.SelectedFlow = flowViewModel;

        await Assert.That(inspectorViewModel.RequestHeadersText).IsEqualTo(string.Empty);
    }

    private RequestReceived CreateRequestEvent()
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri("https://example.com/api/test");
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
        var requestEvent = new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        return requestEvent;
    }

    private sealed class StubDomainEventBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            var subscription = new StubSubscription();
            return subscription;
        }

        private sealed class StubSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}