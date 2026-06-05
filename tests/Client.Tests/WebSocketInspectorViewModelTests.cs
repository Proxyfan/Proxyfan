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
///     Tests for <see cref="WebSocketInspectorViewModel" /> covering selection wiring,
///     event subscription / unsubscription and disposal semantics.
/// </summary>
public sealed class WebSocketInspectorViewModelTests
{
    /// <summary>
    ///     Verifies that an inspector with no selected flow starts inactive and empty.
    /// </summary>
    [Test]
    public async Task State_WhenNoFlowSelected_IsInactive()
    {
        using var harness = CreateHarness();

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsFalse();
        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that selecting a non-WebSocket flow leaves the inspector inactive.
    /// </summary>
    [Test]
    public async Task SelectFlow_NonWebSocketFlow_RemainsInactive()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var flowViewModel = CreateFlowViewModel(flowId);

        harness.TrafficListViewModel.SelectedFlow = flowViewModel;

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsFalse();
    }

    /// <summary>
    ///     Verifies that selecting a flow whose WebSocket store entry exists activates the
    ///     inspector and pre-populates existing messages.
    /// </summary>
    [Test]
    public async Task SelectFlow_WebSocketFlow_ActivatesInspectorAndLoadsMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        var existingMessage = new WebSocketMessage(
            WebSocketDirection.Outbound,
            WebSocketOpcode.Text,
            new byte[] { 1 },
            DateTimeOffset.UtcNow);
        webSocketFlow.RecordMessage(existingMessage);
        var flowViewModel = CreateFlowViewModel(flowId);

        harness.TrafficListViewModel.SelectedFlow = flowViewModel;

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsTrue();
        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo("WebSocket — open");
    }

    /// <summary>
    ///     Verifies that messages recorded on the active flow are appended to the inspector.
    /// </summary>
    [Test]
    public async Task RecordMessage_OnActiveFlow_AppendsToMessageList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        var message = new WebSocketMessage(
            WebSocketDirection.Inbound,
            WebSocketOpcode.Text,
            new byte[] { 2 },
            DateTimeOffset.UtcNow);
        webSocketFlow.RecordMessage(message);

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the inspector updates its connection status when the flow closes.
    /// </summary>
    [Test]
    public async Task FlowClosed_OnActiveFlow_UpdatesStatusToClosed()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo("WebSocket — closed");
    }

    /// <summary>
    ///     Verifies that selecting an already-closed flow reports closed status immediately.
    /// </summary>
    [Test]
    public async Task SelectFlow_AlreadyClosedWebSocket_ReportsClosedStatus()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow);

        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsTrue();
        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo("WebSocket — closed");
    }

    /// <summary>
    ///     Verifies that switching between flows detaches the prior flow so further messages
    ///     on the old flow no longer affect the inspector.
    /// </summary>
    [Test]
    public async Task SelectFlow_SwitchesActiveFlow_DetachesPreviousSubscriptions()
    {
        using var harness = CreateHarness();
        var firstFlowId = Guid.NewGuid();
        var secondFlowId = Guid.NewGuid();
        var firstWebSocket = CreateWebSocketFlow(firstFlowId, harness.WebSocketStore);
        CreateWebSocketFlow(secondFlowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);

        var message = new WebSocketMessage(
            WebSocketDirection.Inbound,
            WebSocketOpcode.Text,
            new byte[] { 9 },
            DateTimeOffset.UtcNow);
        firstWebSocket.RecordMessage(message);

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that clearing selection deactivates the inspector and clears state.
    /// </summary>
    [Test]
    public async Task SelectFlow_ClearedAfterWebSocket_ResetsState()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.TrafficListViewModel.SelectedFlow = null;

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsFalse();
        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that selecting a message populates the detail text.
    /// </summary>
    [Test]
    public async Task SelectedMessage_WithTextPayload_PopulatesDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(new WebSocketMessage(
            WebSocketDirection.Outbound,
            WebSocketOpcode.Text,
            System.Text.Encoding.UTF8.GetBytes("hello"),
            DateTimeOffset.UtcNow));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.SelectedMessage = harness.WebSocketInspector.Messages[0];

        await Assert.That(harness.WebSocketInspector.SelectedMessageDetailText).IsEqualTo("hello");
    }

    /// <summary>
    ///     Verifies that clearing the message selection clears the detail text.
    /// </summary>
    [Test]
    public async Task SelectedMessage_ClearedToNull_ClearsDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(new WebSocketMessage(
            WebSocketDirection.Outbound,
            WebSocketOpcode.Text,
            System.Text.Encoding.UTF8.GetBytes("payload"),
            DateTimeOffset.UtcNow));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.WebSocketInspector.SelectedMessage = harness.WebSocketInspector.Messages[0];

        harness.WebSocketInspector.SelectedMessage = null;

        await Assert.That(harness.WebSocketInspector.SelectedMessageDetailText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that disposal unsubscribes from the underlying flow's events.
    /// </summary>
    [Test]
    public async Task Dispose_AfterActiveFlow_StopsReceivingMessages()
    {
        var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.Dispose();
        webSocketFlow.RecordMessage(new WebSocketMessage(
            WebSocketDirection.Inbound,
            WebSocketOpcode.Text,
            new byte[] { 3 },
            DateTimeOffset.UtcNow));

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketInspectorViewModel.Refresh" /> re-resolves the
    ///     selection when the WebSocket store is populated after selection.
    /// </summary>
    [Test]
    public async Task Refresh_StorePopulatedAfterSelection_ActivatesInspector()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsFalse();

        CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.WebSocketInspector.Refresh();

        await Assert.That(harness.WebSocketInspector.IsWebSocket).IsTrue();
    }

    /// <summary>
    ///     Verifies that filtering by direction outbound hides inbound messages.
    /// </summary>
    [Test]
    public async Task DirectionFilter_Outbound_HidesInboundMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Outbound;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.WebSocketInspector.Messages[0].Message.Direction)
            .IsEqualTo(WebSocketDirection.Outbound);
    }

    /// <summary>
    ///     Verifies that filtering by direction inbound hides outbound messages.
    /// </summary>
    [Test]
    public async Task DirectionFilter_Inbound_HidesOutboundMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Inbound;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.WebSocketInspector.Messages[0].Message.Direction)
            .IsEqualTo(WebSocketDirection.Inbound);
    }

    /// <summary>
    ///     Verifies that the All direction filter shows both directions.
    /// </summary>
    [Test]
    public async Task DirectionFilter_All_ShowsBothDirections()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Inbound;
        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.All;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that the text content filter hides binary and control frames.
    /// </summary>
    [Test]
    public async Task ContentTypeFilter_Text_HidesBinaryAndControlFrames()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Ping));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Pong));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Close));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.ContentTypeFilter = WebSocketContentTypeFilter.Text;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.WebSocketInspector.Messages[0].Message.Opcode)
            .IsEqualTo(WebSocketOpcode.Text);
    }

    /// <summary>
    ///     Verifies that the binary content filter hides text and control frames.
    /// </summary>
    [Test]
    public async Task ContentTypeFilter_Binary_HidesTextAndControlFrames()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Ping));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.ContentTypeFilter = WebSocketContentTypeFilter.Binary;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.WebSocketInspector.Messages[0].Message.Opcode)
            .IsEqualTo(WebSocketOpcode.Binary);
    }

    /// <summary>
    ///     Verifies that the control content filter shows only Ping/Pong/Close frames.
    /// </summary>
    [Test]
    public async Task ContentTypeFilter_Control_ShowsOnlyPingPongClose()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Ping));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Pong));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Close));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.ContentTypeFilter = WebSocketContentTypeFilter.Control;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(3);
    }

    /// <summary>
    ///     Verifies that combining direction + content filters intersects correctly.
    /// </summary>
    [Test]
    public async Task Filters_CombinedDirectionAndContentType_IntersectMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Outbound, WebSocketOpcode.Binary));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Binary));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Outbound;
        harness.WebSocketInspector.ContentTypeFilter = WebSocketContentTypeFilter.Text;

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(1);
        var only = harness.WebSocketInspector.Messages[0].Message;
        await Assert.That(only.Direction).IsEqualTo(WebSocketDirection.Outbound);
        await Assert.That(only.Opcode).IsEqualTo(WebSocketOpcode.Text);
    }

    /// <summary>
    ///     Verifies that new messages recorded while a filter is active are added to the
    ///     visible list only when they match the filter.
    /// </summary>
    [Test]
    public async Task RecordMessage_DoesNotMatchActiveFilter_OmittedFromVisibleList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Outbound;

        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that selecting a message, then applying a filter that hides it, clears
    ///     the selection so the detail pane no longer references a hidden message.
    /// </summary>
    [Test]
    public async Task ApplyFilter_HidesSelectedMessage_ClearsSelection()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var webSocketFlow = CreateWebSocketFlow(flowId, harness.WebSocketStore);
        webSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.WebSocketInspector.SelectedMessage = harness.WebSocketInspector.Messages[0];

        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Outbound;

        await Assert.That(harness.WebSocketInspector.SelectedMessage).IsNull();
        await Assert.That(harness.WebSocketInspector.SelectedMessageDetailText)
            .IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketInspectorViewModel.DirectionFilterIndex" /> is a
    ///     two-way bridge to <see cref="WebSocketInspectorViewModel.DirectionFilter" />.
    /// </summary>
    [Test]
    [Arguments(0, WebSocketDirectionFilter.All)]
    [Arguments(1, WebSocketDirectionFilter.Outbound)]
    [Arguments(2, WebSocketDirectionFilter.Inbound)]
    public async Task DirectionFilterIndex_Setter_UpdatesEnum(int index, WebSocketDirectionFilter expected)
    {
        using var harness = CreateHarness();

        harness.WebSocketInspector.DirectionFilterIndex = index;

        await Assert.That(harness.WebSocketInspector.DirectionFilter).IsEqualTo(expected);
        await Assert.That(harness.WebSocketInspector.DirectionFilterIndex).IsEqualTo(index);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketInspectorViewModel.ContentTypeFilterIndex" /> is
    ///     a two-way bridge to <see cref="WebSocketInspectorViewModel.ContentTypeFilter" />.
    /// </summary>
    [Test]
    [Arguments(0, WebSocketContentTypeFilter.All)]
    [Arguments(1, WebSocketContentTypeFilter.Text)]
    [Arguments(2, WebSocketContentTypeFilter.Binary)]
    [Arguments(3, WebSocketContentTypeFilter.Control)]
    public async Task ContentTypeFilterIndex_Setter_UpdatesEnum(int index, WebSocketContentTypeFilter expected)
    {
        using var harness = CreateHarness();

        harness.WebSocketInspector.ContentTypeFilterIndex = index;

        await Assert.That(harness.WebSocketInspector.ContentTypeFilter).IsEqualTo(expected);
        await Assert.That(harness.WebSocketInspector.ContentTypeFilterIndex).IsEqualTo(index);
    }

    /// <summary>
    ///     Verifies that the index setter clamps negative values to zero
    ///     (matches Avalonia ComboBox semantics when no item is selected).
    /// </summary>
    [Test]
    public async Task DirectionFilterIndex_NegativeValue_ClampedToAll()
    {
        using var harness = CreateHarness();
        harness.WebSocketInspector.DirectionFilter = WebSocketDirectionFilter.Inbound;

        harness.WebSocketInspector.DirectionFilterIndex = -1;

        await Assert.That(harness.WebSocketInspector.DirectionFilter)
            .IsEqualTo(WebSocketDirectionFilter.All);
    }

    /// <summary>
    ///     Verifies that the index setter clamps negative values to zero for the
    ///     content-type filter (matches Avalonia ComboBox semantics).
    /// </summary>
    [Test]
    public async Task ContentTypeFilterIndex_NegativeValue_ClampedToAll()
    {
        using var harness = CreateHarness();
        harness.WebSocketInspector.ContentTypeFilter = WebSocketContentTypeFilter.Binary;

        harness.WebSocketInspector.ContentTypeFilterIndex = -1;

        await Assert.That(harness.WebSocketInspector.ContentTypeFilter)
            .IsEqualTo(WebSocketContentTypeFilter.All);
    }

    /// <summary>
    ///     Verifies that a queued <c>MessageRecorded</c> callback posted by one flow is
    ///     silently dropped when the selected flow changes before the UI-thread queue is
    ///     drained. This guards against stale messages from a previous selection appearing
    ///     under the newly selected flow.
    /// </summary>
    [Test]
    public async Task RecordMessage_PostedCallbackDeferredAcrossFlowSwitch_DropsStaleMessage()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        using var harness = CreateDeferredHarness(scheduler);
        var firstFlowId = Guid.NewGuid();
        var firstWebSocketFlow = CreateWebSocketFlow(firstFlowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);

        firstWebSocketFlow.RecordMessage(CreateMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text));

        var secondFlowId = Guid.NewGuid();
        CreateWebSocketFlow(secondFlowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);
        scheduler.DrainQueue();

        await Assert.That(harness.WebSocketInspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a queued <c>Closed</c> callback posted by one flow is silently
    ///     dropped when the selected flow changes before the UI-thread queue is drained.
    ///     This guards against the closed status of a previous selection overwriting the
    ///     status of the newly selected flow.
    /// </summary>
    [Test]
    public async Task MarkClosed_PostedCallbackDeferredAcrossFlowSwitch_DropsStaleStatus()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        using var harness = CreateDeferredHarness(scheduler);
        var firstFlowId = Guid.NewGuid();
        var firstWebSocketFlow = CreateWebSocketFlow(firstFlowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);

        firstWebSocketFlow.MarkClosed(DateTimeOffset.UtcNow);

        var secondFlowId = Guid.NewGuid();
        CreateWebSocketFlow(secondFlowId, harness.WebSocketStore);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);
        scheduler.DrainQueue();

        await Assert.That(harness.WebSocketInspector.ConnectionStatusText).IsEqualTo("WebSocket — open");
    }

    private static WebSocketMessage CreateMessage(WebSocketDirection direction, WebSocketOpcode opcode)
    {
        var message = new WebSocketMessage(direction, opcode, new byte[] { 1 }, DateTimeOffset.UtcNow);
        return message;
    }

    private static Harness CreateDeferredHarness(DeferredUserInterfaceScheduler scheduler)
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var webSocketStore = new WebSocketStore();
        var webSocketInspector = new WebSocketInspectorViewModel(
            trafficListViewModel,
            webSocketStore,
            scheduler);
        return new Harness(trafficListViewModel, webSocketStore, webSocketInspector);
    }

    private static Harness CreateHarness()
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var webSocketStore = new WebSocketStore();
        var webSocketInspector = new WebSocketInspectorViewModel(
            trafficListViewModel,
            webSocketStore,
            InlineUserInterfaceScheduler.Instance);
        return new Harness(trafficListViewModel, webSocketStore, webSocketInspector);
    }

    private static TrafficFlowViewModel CreateFlowViewModel(Guid flowId)
    {
        var uri = new Uri("https://example.com/socket");
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

    private static WebSocketFlow CreateWebSocketFlow(Guid flowId, WebSocketStore store)
    {
        var underlying = new TrafficFlow(flowId, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var webSocketFlow = new WebSocketFlow(underlying);
        store.Add(webSocketFlow);
        return webSocketFlow;
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

    private sealed class Harness : IDisposable
    {
        public Harness(
            TrafficListViewModel trafficListViewModel,
            WebSocketStore webSocketStore,
            WebSocketInspectorViewModel webSocketInspector)
        {
            TrafficListViewModel = trafficListViewModel;
            WebSocketStore = webSocketStore;
            WebSocketInspector = webSocketInspector;
        }

        public TrafficListViewModel TrafficListViewModel { get; }

        public WebSocketInspectorViewModel WebSocketInspector { get; }

        public WebSocketStore WebSocketStore { get; }

        public void Dispose()
        {
            WebSocketInspector.Dispose();
        }
    }
}
