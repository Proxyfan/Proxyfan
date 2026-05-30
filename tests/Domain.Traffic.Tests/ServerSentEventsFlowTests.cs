using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsFlow" />.
/// </summary>
public sealed class ServerSentEventsFlowTests
{
    /// <summary>
    ///     A fresh flow is open and contains no events.
    /// </summary>
    [Test]
    public async Task Constructor_FreshFlow_IsOpenWithNoEvents()
    {
        var flow = CreateUnderlyingFlow(out _);

        var sseFlow = new ServerSentEventsFlow(flow);

        await Assert.That(sseFlow.IsClosed).IsFalse();
        await Assert.That(sseFlow.ClosedAt).IsNull();
        await Assert.That(sseFlow.Events.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     <see cref="ServerSentEventsFlow.Id" /> mirrors the underlying flow id.
    /// </summary>
    [Test]
    public async Task Id_MirrorsUnderlyingFlow_IsEqual()
    {
        var flow = CreateUnderlyingFlow(out var expectedId);
        var sseFlow = new ServerSentEventsFlow(flow);

        await Assert.That(sseFlow.Id).IsEqualTo(expectedId);
    }

    /// <summary>
    ///     <see cref="ServerSentEventsFlow.MarkClosed(DateTimeOffset)" /> only records the
    ///     first observed close timestamp.
    /// </summary>
    [Test]
    public async Task MarkClosed_CalledTwice_KeepsFirstTimestamp()
    {
        var flow = CreateUnderlyingFlow(out _);
        var sseFlow = new ServerSentEventsFlow(flow);
        var firstClose = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var secondClose = new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero);

        sseFlow.MarkClosed(firstClose);
        sseFlow.MarkClosed(secondClose);

        await Assert.That(sseFlow.IsClosed).IsTrue();
        await Assert.That(sseFlow.ClosedAt).IsEqualTo(firstClose);
    }

    /// <summary>
    ///     <see cref="ServerSentEventsFlow.RecordEvent" /> preserves chronological order.
    /// </summary>
    [Test]
    public async Task RecordEvent_TwoEvents_PreservesOrder()
    {
        var flow = CreateUnderlyingFlow(out _);
        var sseFlow = new ServerSentEventsFlow(flow);
        var firstEvent = new ServerSentEvent("hello", "message", "1", null, DateTimeOffset.UtcNow);
        var secondEvent = new ServerSentEvent("world", "message", "2", null, DateTimeOffset.UtcNow);

        sseFlow.RecordEvent(firstEvent);
        sseFlow.RecordEvent(secondEvent);

        await Assert.That(sseFlow.Events.Count).IsEqualTo(2);
        await Assert.That(sseFlow.Events[0]).IsSameReferenceAs(firstEvent);
        await Assert.That(sseFlow.Events[1]).IsSameReferenceAs(secondEvent);
    }

    /// <summary>
    ///     <see cref="ServerSentEventsFlow.RecordEvent" /> raises
    ///     <see cref="ServerSentEventsFlow.EventRecorded" /> on every append.
    /// </summary>
    [Test]
    public async Task RecordEvent_WithSubscriber_FiresEventRecorded()
    {
        var flow = CreateUnderlyingFlow(out _);
        var sseFlow = new ServerSentEventsFlow(flow);
        var captured = new System.Collections.Generic.List<ServerSentEvent>();
        sseFlow.EventRecorded += serverSentEvent => captured.Add(serverSentEvent);
        var first = new ServerSentEvent("a", null, null, null, DateTimeOffset.UtcNow);
        var second = new ServerSentEvent("b", null, null, null, DateTimeOffset.UtcNow);

        sseFlow.RecordEvent(first);
        sseFlow.RecordEvent(second);

        await Assert.That(captured.Count).IsEqualTo(2);
        await Assert.That(captured[0]).IsSameReferenceAs(first);
        await Assert.That(captured[1]).IsSameReferenceAs(second);
    }

    /// <summary>
    ///     <see cref="ServerSentEventsFlow.MarkClosed(DateTimeOffset)" /> raises the
    ///     <see cref="ServerSentEventsFlow.Closed" /> event on the first observation and
    ///     ignores subsequent calls.
    /// </summary>
    [Test]
    public async Task MarkClosed_CalledTwice_FiresClosedOnlyOnce()
    {
        var flow = CreateUnderlyingFlow(out _);
        var sseFlow = new ServerSentEventsFlow(flow);
        var fireCount = 0;
        sseFlow.Closed += () => fireCount++;

        sseFlow.MarkClosed(DateTimeOffset.UtcNow);
        sseFlow.MarkClosed(DateTimeOffset.UtcNow.AddSeconds(1));

        await Assert.That(fireCount).IsEqualTo(1);
    }

    private static TrafficFlow CreateUnderlyingFlow(out Guid id)
    {
        id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "127.0.0.1:0", DateTimeOffset.UtcNow);
        return flow;
    }
}
