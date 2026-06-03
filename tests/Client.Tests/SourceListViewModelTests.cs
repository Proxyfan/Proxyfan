using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="SourceListViewModel" />.
/// </summary>
public sealed class SourceListViewModelTests
{
    /// <summary>
    ///     Verifies that the source list initially contains only the "All" group.
    /// </summary>
    [Test]
    public async Task Sources_WhenInitialized_ContainsOnlyAllGroup()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);

        await Assert.That(sourceList.Sources.Count).IsEqualTo(1);
        await Assert.That(sourceList.Sources[0].IsAllGroup).IsTrue();
        await Assert.That(sourceList.SelectedSource).IsSameReferenceAs(sourceList.Sources[0]);
    }

    /// <summary>
    ///     Verifies that a new host group is created when a request for an unseen host arrives.
    /// </summary>
    [Test]
    public async Task RequestReceived_NewHost_AddsHostGroup()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);

        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));

        await Assert.That(sourceList.Sources.Count).IsEqualTo(2);
        await Assert.That(sourceList.Sources[1].Host).IsEqualTo("example.com");
        await Assert.That(sourceList.Sources[1].Count).IsEqualTo(1);
        await Assert.That(sourceList.Sources[0].Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that repeat requests for an existing host increment its count.
    /// </summary>
    [Test]
    public async Task RequestReceived_RepeatHost_IncrementsCount()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);

        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        bus.RequestReceivedHandler!(CreateRequestEvent("EXAMPLE.com"));

        await Assert.That(sourceList.Sources.Count).IsEqualTo(2);
        await Assert.That(sourceList.Sources[1].Count).IsEqualTo(3);
        await Assert.That(sourceList.Sources[0].Count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that selecting a host group updates the traffic list's host filter.
    /// </summary>
    [Test]
    public async Task SelectedSource_HostGroupSelected_SetsTrafficListHostFilter()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));

        sourceList.SelectedSource = sourceList.Sources[1];

        await Assert.That(trafficList.HostFilter).IsEqualTo("example.com");
    }

    /// <summary>
    ///     Verifies that selecting the "All" group clears the traffic list's host filter.
    /// </summary>
    [Test]
    public async Task SelectedSource_AllGroupSelected_ClearsTrafficListHostFilter()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        sourceList.SelectedSource = sourceList.Sources[1];

        sourceList.SelectedSource = sourceList.Sources[0];

        await Assert.That(trafficList.HostFilter).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that clearing the traffic list clears all host groups except "All".
    /// </summary>
    [Test]
    public async Task Rebuild_AfterClear_KeepsOnlyAllGroup()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        bus.RequestReceivedHandler!(CreateRequestEvent("other.com"));

        sourceList.Rebuild();

        await Assert.That(sourceList.Sources.Count).IsEqualTo(1);
        await Assert.That(sourceList.Sources[0].IsAllGroup).IsTrue();
        await Assert.That(sourceList.Sources[0].Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that filter is applied to actual traffic flows: only the selected host's flows are visible.
    /// </summary>
    [Test]
    public async Task TrafficList_AfterHostFilter_OnlyShowsMatchingFlows()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        bus.RequestReceivedHandler!(CreateRequestEvent("other.com"));

        sourceList.SelectedSource = sourceList.Sources[1];

        await Assert.That(trafficList.VisibleFlows.Count).IsEqualTo(1);
        await Assert.That(trafficList.VisibleFlows[0].Host).IsEqualTo("example.com");
    }

    /// <summary>
    ///     Verifies that Dispose unsubscribes and does not throw on subsequent collection changes.
    /// </summary>
    [Test]
    public async Task Dispose_AfterDispose_DoesNotThrow()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);

        await Assert.That(() => sourceList.Dispose()).ThrowsNothing();
    }

    /// <summary>
    ///     Verifies that the request URI host is used when the Host header is missing.
    /// </summary>
    [Test]
    public async Task RequestReceived_MissingHostHeader_FallsBackToUriHost()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);

        bus.RequestReceivedHandler!(CreateRequestEventWithoutHostHeader("uri-host.test"));

        await Assert.That(sourceList.Sources.Count).IsEqualTo(2);
        await Assert.That(sourceList.Sources[1].Host).IsEqualTo("uri-host.test");
    }

    /// <summary>
    ///     Verifies that the source list rebuilds when the traffic list
    ///     clears its flow collection (via the shared coordinator) without
    ///     the source list holding a direct reference to the traffic list.
    /// </summary>
    [Test]
    public async Task TrafficListClear_PublishedViaCoordinator_RebuildsSourceList()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));
        bus.RequestReceivedHandler!(CreateRequestEvent("other.com"));

        trafficList.ClearCommand.Execute(null);

        await Assert.That(sourceList.Sources.Count).IsEqualTo(1);
        await Assert.That(sourceList.Sources[0].IsAllGroup).IsTrue();
    }

    /// <summary>
    ///     Verifies that selecting a host group propagates to the traffic
    ///     list's host filter through the shared coordinator, without the
    ///     source list holding a direct reference to the traffic list.
    /// </summary>
    [Test]
    public async Task SelectedSource_PublishedViaCoordinator_UpdatesTrafficListHostFilter()
    {
        var bus = new RecordingEventBus();
        var coordinator = new TrafficListCoordinator();
        using var trafficList = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance, requestRepeater: null, diffPool: null, clipboardService: null, coordinator: coordinator);
        using var sourceList = new SourceListViewModel(bus, coordinator, InlineUserInterfaceScheduler.Instance);
        bus.RequestReceivedHandler!(CreateRequestEvent("example.com"));

        sourceList.SelectedSource = sourceList.Sources[1];

        await Assert.That(trafficList.HostFilter).IsEqualTo("example.com");
    }

    private static RequestReceived CreateRequestEvent(string host)
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri($"https://{host}/api");
        var headers = HeaderCollection.Empty.Add("Host", host);
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

    private static RequestReceived CreateRequestEventWithoutHostHeader(string uriHost)
    {
        var flowId = Guid.NewGuid();
        var uri = new Uri($"https://{uriHost}/api");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty,
            Method = "GET",
            RequestUri = uri,
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return new RequestReceived(flowId, request, "127.0.0.1:9000", DateTimeOffset.UtcNow);
    }

    private sealed class RecordingEventBus : IDomainEventBus
    {
        private readonly System.Collections.Generic.List<DomainEventHandler<RequestReceived>> _requestHandlers = [];

        public DomainEventHandler<RequestReceived>? RequestReceivedHandler
        {
            get
            {
                if (_requestHandlers.Count == 0)
                {
                    return null;
                }

                return DispatchRequest;
            }
        }

        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (typeof(TEvent) == typeof(RequestReceived) && handler is DomainEventHandler<RequestReceived> requestHandler)
            {
                _requestHandlers.Add(requestHandler);
            }

            return new NoopSubscription();
        }

        private void DispatchRequest(RequestReceived domainEvent)
        {
            foreach (var handler in _requestHandlers)
            {
                handler(domainEvent);
            }
        }

        private sealed class NoopSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
