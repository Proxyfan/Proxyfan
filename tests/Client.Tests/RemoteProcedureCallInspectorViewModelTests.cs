using Proxyfan.Client.Inspector.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Client.Traffic.ViewModels;
using Proxyfan.Client.Tools;
using Proxyfan.Domain;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using Proxyfan.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallInspectorViewModel" /> covering selection
///     wiring, event subscription / unsubscription, direction filter, and disposal semantics.
/// </summary>
public sealed class RemoteProcedureCallInspectorViewModelTests
{
    /// <summary>
    ///     Verifies that an inspector with no selected flow starts inactive and empty.
    /// </summary>
    [Test]
    public async Task State_WhenNoFlowSelected_IsInactive()
    {
        using var harness = CreateHarness();

        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsFalse();
        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(0);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Selecting a non-gRPC flow leaves the inspector inactive.
    /// </summary>
    [Test]
    public async Task SelectFlow_NonRemoteProcedureCallFlow_RemainsInactive()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsFalse();
    }

    /// <summary>
    ///     Selecting a gRPC-store-backed flow activates the inspector and loads existing messages.
    /// </summary>
    [Test]
    public async Task SelectFlow_RemoteProcedureCallFlow_ActivatesInspectorAndLoadsMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));

        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsTrue();
        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(1);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo("gRPC — streaming");
    }

    /// <summary>
    ///     Messages recorded on the active flow are appended to the inspector.
    /// </summary>
    [Test]
    public async Task RecordMessage_OnActiveFlow_AppendsToMessageList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Inbound, new byte[] { 0x02 }));

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Closing the active flow updates the connection status text.
    /// </summary>
    [Test]
    public async Task MarkClosed_ActiveFlow_UpdatesStatusText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        grpcFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo("gRPC — closed");
    }

    /// <summary>
    ///     Switching flows detaches event subscription from the previous one.
    /// </summary>
    [Test]
    public async Task SelectFlow_SwitchingFlows_DetachesFromPreviousFlow()
    {
        using var harness = CreateHarness();
        var firstFlowId = Guid.NewGuid();
        var firstGrpcFlow = CreateRemoteProcedureCallFlow(firstFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);
        var secondFlowId = Guid.NewGuid();
        CreateRemoteProcedureCallFlow(secondFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);

        firstGrpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0xFF }));

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Clearing selection deactivates the inspector and clears state.
    /// </summary>
    [Test]
    public async Task SelectFlow_ClearedAfterRemoteProcedureCall_ResetsState()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.TrafficListViewModel.SelectedFlow = null;

        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsFalse();
        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(0);
        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Selecting a message populates the detail text.
    /// </summary>
    [Test]
    public async Task SelectedMessage_WithPayload_PopulatesDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x10, 0x20 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.SelectedMessage = harness.Inspector.Messages[0];

        await Assert.That(harness.Inspector.SelectedMessageDetailText).Contains("Outbound");
        await Assert.That(harness.Inspector.SelectedMessageDetailText).Contains("10 20");
    }

    /// <summary>
    ///     Clearing the message selection clears the detail text.
    /// </summary>
    [Test]
    public async Task SelectedMessage_ClearedToNull_ClearsDetailText()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x10 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.SelectedMessage = harness.Inspector.Messages[0];

        harness.Inspector.SelectedMessage = null;

        await Assert.That(harness.Inspector.SelectedMessageDetailText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     The Outbound direction filter hides inbound messages.
    /// </summary>
    [Test]
    public async Task DirectionFilter_OutboundOnly_HidesInboundMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Inbound, new byte[] { 0x02 }));
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x03 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.DirectionFilter = "Outbound";

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     The Inbound direction filter hides outbound messages.
    /// </summary>
    [Test]
    public async Task DirectionFilter_InboundOnly_HidesOutboundMessages()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Inbound, new byte[] { 0x02 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.DirectionFilter = "Inbound";

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Filtering hides the currently-selected message and clears selection.
    /// </summary>
    [Test]
    public async Task DirectionFilter_HidesSelectedMessage_ClearsSelection()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.SelectedMessage = harness.Inspector.Messages[0];

        harness.Inspector.DirectionFilter = "Inbound";

        await Assert.That(harness.Inspector.SelectedMessage).IsNull();
        await Assert.That(harness.Inspector.SelectedMessageDetailText).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     A live-recorded message that doesn't match the filter is omitted from the visible list.
    /// </summary>
    [Test]
    public async Task RecordMessage_DoesNotMatchActiveFilter_OmittedFromVisibleList()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        harness.Inspector.DirectionFilter = "Outbound";

        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Inbound, new byte[] { 0x01 }));

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Refresh re-evaluates a late store insert without a flow re-selection.
    /// </summary>
    [Test]
    public async Task Refresh_AfterLateStoreInsert_ActivatesInspector()
    {
        using var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsFalse();

        CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.Inspector.Refresh();

        await Assert.That(harness.Inspector.IsRemoteProcedureCall).IsTrue();
    }

    /// <summary>
    ///     Disposal unsubscribes from the active flow's events.
    /// </summary>
    [Test]
    public async Task Dispose_AfterActiveFlow_StopsReceivingMessages()
    {
        var harness = CreateHarness();
        var flowId = Guid.NewGuid();
        var grpcFlow = CreateRemoteProcedureCallFlow(flowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);
        var inspector = harness.Inspector;
        harness.Dispose();

        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0xFF }));

        await Assert.That(inspector.Messages.Count).IsEqualTo(0);
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
        var firstGrpcFlow = CreateRemoteProcedureCallFlow(firstFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);

        firstGrpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x01 }));

        var secondFlowId = Guid.NewGuid();
        CreateRemoteProcedureCallFlow(secondFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);
        scheduler.DrainQueue();

        await Assert.That(harness.Inspector.Messages.Count).IsEqualTo(0);
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
        var firstGrpcFlow = CreateRemoteProcedureCallFlow(firstFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(firstFlowId);

        firstGrpcFlow.MarkClosed(DateTimeOffset.UtcNow);

        var secondFlowId = Guid.NewGuid();
        CreateRemoteProcedureCallFlow(secondFlowId, harness.Store);
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(secondFlowId);
        scheduler.DrainQueue();

        await Assert.That(harness.Inspector.ConnectionStatusText).IsEqualTo("gRPC — streaming");
    }

    /// <summary>
    ///     When the inspector is wired with a descriptor library that knows the gRPC method
    ///     of the active flow, the SelectedMessageDetailText includes a "Schema    :" line.
    /// </summary>
    [Test]
    public async Task SelectedMessage_WithMatchingDescriptor_RendersSchemaLine()
    {
        var library = BuildLibraryWithSayHelloMethod();
        using var harness = CreateHarnessWithLibrary(library);
        var flowId = Guid.NewGuid();
        var underlying = new TrafficFlow(flowId, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        underlying.SetRequest(BuildRequest("https://example.com/demo.Greeter/SayHello"));
        var grpcFlow = new RemoteProcedureCallFlow(underlying);
        harness.Store.Add(grpcFlow);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x08, 0x05 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.SelectedMessage = harness.Inspector.Messages[0];

        await Assert.That(harness.Inspector.SelectedMessageDetailText).Contains("Schema    : .demo.HelloRequest");
        await Assert.That(harness.Inspector.SelectedMessageDetailText).Contains("Decoded protobuf (schema):");
    }

    /// <summary>
    ///     When the descriptor library has no entry for the flow's method path, the inspector
    ///     falls back to the schema-less decoder (no "Schema    :" line).
    /// </summary>
    [Test]
    public async Task SelectedMessage_LibraryWithoutMatchingMethod_FallsBackToSchemaless()
    {
        var library = new RemoteProcedureCallDescriptorLibrary();
        using var harness = CreateHarnessWithLibrary(library);
        var flowId = Guid.NewGuid();
        var underlying = new TrafficFlow(flowId, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        underlying.SetRequest(BuildRequest("https://example.com/unknown.Service/Method"));
        var grpcFlow = new RemoteProcedureCallFlow(underlying);
        harness.Store.Add(grpcFlow);
        grpcFlow.RecordMessage(CreateMessage(RemoteProcedureCallDirection.Outbound, new byte[] { 0x08, 0x05 }));
        harness.TrafficListViewModel.SelectedFlow = CreateFlowViewModel(flowId);

        harness.Inspector.SelectedMessage = harness.Inspector.Messages[0];

        await Assert.That(harness.Inspector.SelectedMessageDetailText.Contains("Schema    :")).IsFalse();
    }

    private static InMemoryDescriptorLibrary BuildLibraryWithSayHelloMethod()
    {
        var fieldDescriptor = new ProtobufFieldDescriptor
        {
            Kind = ProtobufFieldKind.TypeInt32,
            Label = ProtobufFieldLabel.Optional,
            Name = "id",
            Number = 1,
        };
        var helloRequest = new ProtobufMessageDescriptor
        {
            Fields = new List<ProtobufFieldDescriptor> { fieldDescriptor },
            FullName = ".demo.HelloRequest",
            Name = "HelloRequest",
            NestedEnums = Array.Empty<ProtobufEnumDescriptor>(),
            NestedMessages = Array.Empty<ProtobufMessageDescriptor>(),
        };
        var helloReply = new ProtobufMessageDescriptor
        {
            Fields = Array.Empty<ProtobufFieldDescriptor>(),
            FullName = ".demo.HelloReply",
            Name = "HelloReply",
            NestedEnums = Array.Empty<ProtobufEnumDescriptor>(),
            NestedMessages = Array.Empty<ProtobufMessageDescriptor>(),
        };
        var method = new ProtobufMethodDescriptor
        {
            FullPath = "/demo.Greeter/SayHello",
            InputType = ".demo.HelloRequest",
            IsClientStreaming = false,
            IsServerStreaming = false,
            Name = "SayHello",
            OutputType = ".demo.HelloReply",
        };
        var service = new ProtobufServiceDescriptor
        {
            FullName = ".demo.Greeter",
            Methods = new List<ProtobufMethodDescriptor> { method },
            Name = "Greeter",
        };
        var file = new ProtobufFileDescriptor
        {
            Enums = Array.Empty<ProtobufEnumDescriptor>(),
            Messages = new List<ProtobufMessageDescriptor> { helloRequest, helloReply },
            Name = "greeter.proto",
            Package = "demo",
            Services = new List<ProtobufServiceDescriptor> { service },
        };
        return new InMemoryDescriptorLibrary(new List<ProtobufFileDescriptor> { file });
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string uri)
    {
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = HeaderCollection.Empty.Add("content-type", "application/grpc"),
            Method = "POST",
            RequestUri = new Uri(uri),
            Version = "HTTP/2",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return request;
    }

    private static Harness CreateHarnessWithLibrary(IRemoteProcedureCallDescriptorLibrary library)
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var store = new RemoteProcedureCallStore();
        var schemaLibrary = new RemoteProcedureCallSchemaLibraryAdapter(library);
        var inspector = new RemoteProcedureCallInspectorViewModel(
            trafficListViewModel,
            store,
            InlineUserInterfaceScheduler.Instance,
            schemaLibrary);
        return new Harness(trafficListViewModel, store, inspector);
    }

    private static RemoteProcedureCallCapturedMessage CreateMessage(RemoteProcedureCallDirection direction, byte[] payload)
    {
        var message = new RemoteProcedureCallCapturedMessage(direction, false, payload, DateTimeOffset.UtcNow);
        return message;
    }

    private static TrafficFlowViewModel CreateFlowViewModel(Guid flowId)
    {
        var uri = new Uri("https://example.com/rpc");
        var headers = HeaderCollection.Empty.Add("Host", "example.com");
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = Array.Empty<byte>(),
            Headers = headers,
            Method = "POST",
            RequestUri = uri,
            Version = "HTTP/2",
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
        var store = new RemoteProcedureCallStore();
        var inspector = new RemoteProcedureCallInspectorViewModel(
            trafficListViewModel,
            store,
            InlineUserInterfaceScheduler.Instance);
        return new Harness(trafficListViewModel, store, inspector);
    }

    private static Harness CreateDeferredHarness(DeferredUserInterfaceScheduler scheduler)
    {
        var bus = new StubDomainEventBus();
        var trafficListViewModel = new TrafficListViewModel(bus, InlineUserInterfaceScheduler.Instance);
        var store = new RemoteProcedureCallStore();
        var inspector = new RemoteProcedureCallInspectorViewModel(
            trafficListViewModel,
            store,
            scheduler);
        return new Harness(trafficListViewModel, store, inspector);
    }

    private static RemoteProcedureCallFlow CreateRemoteProcedureCallFlow(Guid flowId, IRemoteProcedureCallStore store)
    {
        var underlying = new TrafficFlow(flowId, "127.0.0.1:9000", DateTimeOffset.UtcNow);
        var grpcFlow = new RemoteProcedureCallFlow(underlying);
        store.Add(grpcFlow);
        return grpcFlow;
    }

    private sealed class Harness : IDisposable
    {
        public RemoteProcedureCallInspectorViewModel Inspector { get; }

        public IRemoteProcedureCallStore Store { get; }

        public TrafficListViewModel TrafficListViewModel { get; }

        public Harness(TrafficListViewModel trafficListViewModel, IRemoteProcedureCallStore store, RemoteProcedureCallInspectorViewModel inspector)
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

    private sealed class InMemoryDescriptorLibrary : IRemoteProcedureCallDescriptorLibrary
    {
        public InMemoryDescriptorLibrary(IReadOnlyList<ProtobufFileDescriptor> files)
        {
            Index = new ProtobufDescriptorIndex(files);
            LoadedFilePaths = Array.Empty<string>();
        }

        public ProtobufDescriptorIndex Index { get; }

        public IReadOnlyList<string> LoadedFilePaths { get; }

        public void Clear()
        {
        }

        public void Load(string sourcePath, byte[] payload)
        {
            _ = sourcePath;
            _ = payload;
        }

        public void Unload(string sourcePath)
        {
            _ = sourcePath;
        }
    }
}
