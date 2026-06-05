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
///     Tests for <see cref="ServerSentEventsInspectorViewModel" /> covering selection wiring,
///     event subscription / unsubscription, filtering, and disposal semantics.
/// </summary>
public sealed class ServerSentEventsInspectorViewModelTests
{
    /// <summary>
    ///     Verifies that attaching to a flow with multiple pre-existing events seeds the
    ///     inspector from a stable snapshot without losing or duplicating any event row.
    /// </summary>
    [Test]
    public async Task AttachFlow_MultipleEventsAlreadyRecorded_AllSeedFromSnapshotWithoutDuplicates()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("update", "1"));
        sseFlow.RecordEvent(CreateEvent("update", "2"));
        sseFlow.RecordEvent(CreateEvent("update", "3"));

        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that if <see cref="ServerSentEventsFlow.EventRecorded" /> fires for a
    ///     snapshot-era event after the handler is subscribed (the race that can occur because
    ///     the event fires outside the producer lock), the deduplication set prevents a
    ///     double entry in the inspector.
    /// </summary>
    [Test]
    public async Task AttachFlow_SnapshotEventArrivingViaHandlerAfterAttach_IsDeduplicatedAndNotAdded()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        var e1 = CreateEvent("seed", "before-attach");
        sseFlow.RecordEvent(e1);

        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(1);

        sseFlow.RecordEvent(e1);

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an inspector with no selected flow starts inactive and empty.
    /// </summary>
    [Test]
    public async Task State_WhenNoFlowSelected_IsInactive()
    {
        using var harness = CreateHarness();

        await Assert.That(harness.Inspector.IsServerSentEvents).IsFalse();
        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(0);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that selecting a non-SSE flow leaves the inspector inactive.
    /// </summary>
    [Test]
    public async Task SelectFlow_NonServerSentEventsFlow_RemainsInactive()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.Inspector.IsServerSentEvents).IsFalse();
    }

    /// <summary>
    ///     Verifies that selecting a flow whose SSE store entry exists activates the
    ///     inspector and pre-populates existing events.
    /// </summary>
    [Test]
    public async Task SelectFlow_ServerSentEventsFlow_ActivatesInspectorAndLoadsEvents()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("greeting", "hello"));

        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.Inspector.IsServerSentEvents).IsTrue();
        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(1);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo("Server-Sent Events — streaming");
    }

    /// <summary>
    ///     Verifies that events recorded on the active flow are appended to the inspector.
    /// </summary>
    [Test]
    public async Task RecordEvent_OnActiveFlow_AppendsToEventList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        sseFlow.RecordEvent(CreateEvent("tick", "1"));
        sseFlow.RecordEvent(CreateEvent("tick", "2"));

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that closing the flow updates the connection status text.
    /// </summary>
    [Test]
    public async Task MarkClosed_ActiveFlow_UpdatesStatusText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        sseFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo("Server-Sent Events — closed");
    }

    /// <summary>
    ///     Verifies that selecting a different flow detaches event subscription from the
    ///     previous one.
    /// </summary>
    [Test]
    public async Task SelectFlow_SwitchingFlows_DetachesFromPreviousFlow()
    {
        using var harness = CreateHarness();
        var firstFlowId = Guid.NewGuid();
        var firstSseFlow = CreateServerSentEventsFlow(firstFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);
        var secondFlowId = Guid.NewGuid();
        CreateServerSentEventsFlow(secondFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);

        firstSseFlow.RecordEvent(CreateEvent("ignored", "data"));

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that clearing selection deactivates the inspector and clears state.
    /// </summary>
    [Test]
    public async Task SelectFlow_ClearedAfterServerSentEvents_ResetsState()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        CreateServerSentEventsFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.TrafficListViewModel.SelectedFlow = null;

        await Assert.That(harness.Inspector.IsServerSentEvents).IsFalse();
        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(0);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that selecting an event populates the detail text.
    /// </summary>
    [Test]
    public async Task SelectedEvent_WithDataPayload_PopulatesDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("ping", "payload-bytes"));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.SelectedEvent = harness.Inspector.Events[0];

        await Assert.That(harness.Inspector.SelectedEventDetailText).Contains("payload-bytes");
        await Assert.That(harness.Inspector.SelectedEventDetailText).Contains("ping");
    }

    /// <summary>
    ///     Verifies that clearing the event selection clears the detail text.
    /// </summary>
    [Test]
    public async Task SelectedEvent_ClearedToNull_ClearsDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("greeting", "hello"));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.SelectedEvent = harness.Inspector.Events[0];

        harness.Inspector.SelectedEvent = null;

        await Assert.That(harness.Inspector.SelectedEventDetailText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that the event-type filter hides non-matching events.
    /// </summary>
    [Test]
    public async Task EventTypeFilter_PartialMatch_HidesNonMatchingEvents()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("tick", "1"));
        sseFlow.RecordEvent(CreateEvent("notification", "2"));
        sseFlow.RecordEvent(CreateEvent("tick", "3"));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.EventTypeFilter = "tick";

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that filtering hides the currently-selected event and clears the
    ///     selection (so the detail pane no longer references a hidden event).
    /// </summary>
    [Test]
    public async Task EventTypeFilter_HidesSelectedEvent_ClearsSelection()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        sseFlow.RecordEvent(CreateEvent("tick", "1"));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.SelectedEvent = harness.Inspector.Events[0];

        harness.Inspector.EventTypeFilter = "other";

        await Assert.That(harness.Inspector.SelectedEvent).IsNull();
        await Assert.That(harness.Inspector.SelectedEventDetailText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that events recorded while a filter is active are added only when
    ///     they match.
    /// </summary>
    [Test]
    public async Task RecordEvent_DoesNotMatchActiveFilter_OmittedFromVisibleList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.EventTypeFilter = "tick";

        sseFlow.RecordEvent(CreateEvent("other", "x"));

        await Assert.That(harness.Inspector.Events.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="ServerSentEventsInspectorViewModel.Refresh" /> re-evaluates
    ///     the selection so a late SSE-store add becomes visible without a flow re-selection.
    /// </summary>
    [Test]
    public async Task Refresh_AfterLateStoreInsert_ActivatesInspector()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        await Assert.That(harness.Inspector.IsServerSentEvents).IsFalse();

        CreateServerSentEventsFlow(flowId, harness.Store);
        harness.Inspector.Refresh();

        await Assert.That(harness.Inspector.IsServerSentEvents).IsTrue();
    }

    /// <summary>
    ///     Verifies that disposal unsubscribes from the active flow's events.
    /// </summary>
    [Test]
    public async Task Dispose_AfterActiveFlow_StopsReceivingEvents()
    {
        var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var sseFlow = CreateServerSentEventsFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        var inspector = harness.Inspector;
        harness.Dispose();

        sseFlow.RecordEvent(CreateEvent("ignored", "after-dispose"));

        await Assert.That(inspector.Events.Count).IsEqualTo(0);
    }

    private static ServerSentEvent CreateEvent(string eventType, string data)
    {
        var serverSentEvent = new ServerSentEvent(data, eventType, id: null, retryMilliseconds: null, timestamp: DateTimeOffset.UtcNow);
        return serverSentEvent;
    }

    private static TrafficFlowViewModel CreateFlowViewModel(Guid flowId)
    {
        var uri = new Uri("https://example.com/events");
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
        var viewModel = new TrafficFlowViewModel(requestEvent, 1);
        return viewModel;
    }

    private static Harness CreateHarness()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var store = new ServerSentEventsStore();
        var inspector = new ServerSentEventsInspectorViewModel(
            trafficListViewModel,
            store,
            InlineUserInterfaceScheduler.Instance);
        return new Harness(trafficListViewModel, store, inspector);
    }

    private static ServerSentEventsFlow CreateServerSentEventsFlow(Guid flowId, IServerSentEventsStore store)
    {
        var underlying = new TrafficFlow(flowId, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var sseFlow = new ServerSentEventsFlow(underlying);
        store.Add(sseFlow);
        return sseFlow;
    }

    private sealed class Harness : IDisposable
    {
        public ServerSentEventsInspectorViewModel Inspector { get; }

        public IServerSentEventsStore Store { get; }

        public TrafficListViewModel TrafficListViewModel { get; }

        public Harness(TrafficListViewModel trafficListViewModel, IServerSentEventsStore store, ServerSentEventsInspectorViewModel inspector)
        {
            TrafficListViewModel = trafficListViewModel;
            Store = store;
            Inspector = inspector;
        }

        public void Dispose()
        {
            Inspector.Dispose();
            TrafficListViewModel.Dispose();
        }
    }

    private sealed class StubDomainEventBus : IDomainEventBus
    {
        public void Publish<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
            _ = domainEvent;
        }

        public IDisposable Subscribe<TEvent>(DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            _ = handler;
            return new NoOpDisposable();
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
