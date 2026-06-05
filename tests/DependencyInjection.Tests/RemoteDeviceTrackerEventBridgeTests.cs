using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain;
using Proxyfan.Domain.RemoteDevices;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.DependencyInjection.Tests;

public sealed class RemoteDeviceTrackerEventBridgeTests
{
    [Test]
    public async Task RequestReceived_KnownClient_RecordsDeviceOnTracker()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var tracker = new RemoteDeviceTracker();
        using (var bridge = new RemoteDeviceTrackerEventBridge(bus, tracker))
        {
            var request = BuildRequest("Mozilla/5.0 (iPhone)");
            bus.Publish(new RequestReceived(Guid.NewGuid(), request, "10.0.0.5:54321", DateTimeOffset.UtcNow));
        }

        var snapshot = tracker.Snapshot();
        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot[0].Address).IsEqualTo("10.0.0.5");
        await Assert.That(snapshot[0].Kind).IsEqualTo(RemoteDeviceKind.Ios);
    }

    [Test]
    public async Task RequestReceived_EmptyClientEndPoint_IsIgnored()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var tracker = new RemoteDeviceTracker();
        using (new RemoteDeviceTrackerEventBridge(bus, tracker))
        {
            var request = BuildRequest("curl/8.0");
            bus.Publish(new RequestReceived(Guid.NewGuid(), request, string.Empty, DateTimeOffset.UtcNow));
        }

        await Assert.That(tracker.Snapshot().Count).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_AfterCall_UnsubscribesFromBus()
    {
        var bus = new DomainEventBus(NullLogger<DomainEventBus>.Instance);
        var tracker = new RemoteDeviceTracker();
        var bridge = new RemoteDeviceTrackerEventBridge(bus, tracker);
        bridge.Dispose();
        var request = BuildRequest("curl/8.0");
        bus.Publish(new RequestReceived(Guid.NewGuid(), request, "10.0.0.1:80", DateTimeOffset.UtcNow));
        await Assert.That(tracker.Snapshot().Count).IsEqualTo(0);
    }

    private static HypertextTransferProtocolRequestData BuildRequest(string userAgent)
    {
        var headers = HeaderCollection.Empty.Add("User-Agent", userAgent);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = ReadOnlyMemory<byte>.Empty,
            Headers = headers,
            Method = "GET",
            RequestUri = new Uri("http://example.com/"),
            Version = "HTTP/1.1",
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return request;
    }
}
