using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="WebSocketFlow" /> covering message capture, close tracking
///     and thread-safe state mutation.
/// </summary>
public sealed class WebSocketFlowTests
{
    /// <summary>
    ///     Verifies that <see cref="WebSocketFlow.Id" /> mirrors the underlying flow id.
    /// </summary>
    [Test]
    public async Task Id_WhenConstructed_MatchesUnderlyingFlow()
    {
        var underlying = CreateUnderlyingFlow(out var expectedId);
        var webSocketFlow = new WebSocketFlow(underlying);

        await Assert.That(webSocketFlow.Id).IsEqualTo(expectedId);
    }

    /// <summary>
    ///     Verifies a freshly-created WebSocket flow starts open with no messages.
    /// </summary>
    [Test]
    public async Task State_WhenConstructed_IsOpenWithNoMessages()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);

        await Assert.That(webSocketFlow.IsClosed).IsFalse();
        await Assert.That(webSocketFlow.ClosedAt).IsNull();
        await Assert.That(webSocketFlow.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that message snapshots are point-in-time copies and are not mutated
    ///     by later recordings.
    /// </summary>
    [Test]
    public async Task GetMessageSnapshot_AfterNewRecord_RemainsStablePointInTime()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var first = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);
        var second = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 2 }, DateTimeOffset.UtcNow);
        webSocketFlow.RecordMessage(first);

        var snapshot = webSocketFlow.GetMessageSnapshot();
        webSocketFlow.RecordMessage(second);

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot[0]).IsSameReferenceAs(first);
        await Assert.That(webSocketFlow.Messages.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that recorded messages appear in chronological order.
    /// </summary>
    [Test]
    public async Task RecordMessage_MultipleMessages_PreservesOrder()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var first = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);
        var second = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 2 }, DateTimeOffset.UtcNow);

        webSocketFlow.RecordMessage(first);
        webSocketFlow.RecordMessage(second);

        await Assert.That(webSocketFlow.Messages.Count).IsEqualTo(2);
        await Assert.That(webSocketFlow.Messages[0]).IsSameReferenceAs(first);
        await Assert.That(webSocketFlow.Messages[1]).IsSameReferenceAs(second);
    }

    /// <summary>
    ///     Verifies that appending past the per-flow capacity evicts the oldest message and
    ///     increments the dropped counter.
    /// </summary>
    [Test]
    public async Task RecordMessage_WebSocketBeyondCapacity_EvictsOldest()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var budget = new StreamingCaptureBudget(1024);
        var webSocketFlow = new WebSocketFlow(underlying, 2, budget);
        var first = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);
        var second = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 2 }, DateTimeOffset.UtcNow);
        var third = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, new byte[] { 3 }, DateTimeOffset.UtcNow);

        webSocketFlow.RecordMessage(first);
        webSocketFlow.RecordMessage(second);
        webSocketFlow.RecordMessage(third);

        await Assert.That(webSocketFlow.Messages.Count).IsEqualTo(2);
        await Assert.That(webSocketFlow.Messages[0]).IsSameReferenceAs(second);
        await Assert.That(webSocketFlow.Messages[1]).IsSameReferenceAs(third);
        await Assert.That(webSocketFlow.DroppedMessagesCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a shared global streaming budget drops new captures once exhausted.
    /// </summary>
    [Test]
    public async Task RecordMessage_GlobalStreamingBudgetExceeded_DropsMessage()
    {
        var firstUnderlying = CreateUnderlyingFlow(out _);
        var secondUnderlying = CreateUnderlyingFlow(out _);
        var budget = new StreamingCaptureBudget(3);
        var firstFlow = new WebSocketFlow(firstUnderlying, 2, budget);
        var secondFlow = new WebSocketFlow(secondUnderlying, 2, budget);
        var firstMessage = new WebSocketMessage(WebSocketDirection.Outbound, WebSocketOpcode.Text, new byte[] { 1, 2, 3 }, DateTimeOffset.UtcNow);
        var secondMessage = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 4 }, DateTimeOffset.UtcNow);

        firstFlow.RecordMessage(firstMessage);
        secondFlow.RecordMessage(secondMessage);

        await Assert.That(firstFlow.Messages.Count).IsEqualTo(1);
        await Assert.That(secondFlow.Messages.Count).IsEqualTo(0);
        await Assert.That(secondFlow.DroppedMessagesCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the first <see cref="WebSocketFlow.MarkClosed" /> wins and
    ///     subsequent calls are no-ops.
    /// </summary>
    [Test]
    public async Task MarkClosed_CalledTwice_KeepsFirstTimestamp()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var firstClose = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var secondClose = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);

        webSocketFlow.MarkClosed(firstClose);
        webSocketFlow.MarkClosed(secondClose);

        await Assert.That(webSocketFlow.IsClosed).IsTrue();
        await Assert.That(webSocketFlow.ClosedAt).IsEqualTo(firstClose);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketFlow.MessageRecorded" /> fires once per recorded
    ///     message and the handler receives the recorded message instance.
    /// </summary>
    [Test]
    public async Task RecordMessage_WithSubscriber_FiresEventOncePerMessage()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var captured = new List<WebSocketMessage>();
        webSocketFlow.MessageRecorded += captured.Add;
        var message = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);

        webSocketFlow.RecordMessage(message);

        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(captured[0]).IsSameReferenceAs(message);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketFlow.Closed" /> fires exactly once even when
    ///     <see cref="WebSocketFlow.MarkClosed" /> is invoked multiple times.
    /// </summary>
    [Test]
    public async Task MarkClosed_WithSubscriber_FiresEventOnce()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var fireCount = 0;
        webSocketFlow.Closed += () => fireCount++;

        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow);
        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow.AddSeconds(1));

        await Assert.That(fireCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the <see cref="WebSocketFlow.MessageRecorded" /> event handler is
    ///     invoked outside the internal lock so it can call back into the flow without deadlocking.
    /// </summary>
    [Test]
    public async Task RecordMessage_HandlerReentersFlow_DoesNotDeadlock()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var reentrantCount = 0;
        webSocketFlow.MessageRecorded += _ => reentrantCount = webSocketFlow.Messages.Count;

        var message = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);
        webSocketFlow.RecordMessage(message);

        await Assert.That(reentrantCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that <see cref="WebSocketFlow.MarkClosed" /> handlers can read the
    ///     <see cref="WebSocketFlow.IsClosed" /> state without deadlocking.
    /// </summary>
    [Test]
    public async Task MarkClosed_HandlerReadsState_DoesNotDeadlock()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var observedClosed = false;
        webSocketFlow.Closed += () => observedClosed = webSocketFlow.IsClosed;

        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(observedClosed).IsTrue();
    }

    /// <summary>
    ///     Verifies that an exception thrown by a <see cref="WebSocketFlow.MessageRecorded" />
    ///     subscriber is isolated and does not propagate into the capture path nor prevent
    ///     subsequent subscribers from being invoked.
    /// </summary>
    [Test]
    public async Task RecordMessage_FaultySubscriber_DoesNotPropagateAndStillInvokesOthers()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var laterSubscriberInvoked = false;
        webSocketFlow.MessageRecorded += _ => throw new InvalidOperationException("boom");
        webSocketFlow.MessageRecorded += _ => laterSubscriberInvoked = true;
        var message = new WebSocketMessage(WebSocketDirection.Inbound, WebSocketOpcode.Text, new byte[] { 1 }, DateTimeOffset.UtcNow);

        webSocketFlow.RecordMessage(message);

        await Assert.That(laterSubscriberInvoked).IsTrue();
        await Assert.That(webSocketFlow.Messages.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an exception thrown by a <see cref="WebSocketFlow.Closed" />
    ///     subscriber is isolated and does not propagate into the capture path nor prevent
    ///     subsequent subscribers from being invoked.
    /// </summary>
    [Test]
    public async Task MarkClosed_FaultySubscriber_DoesNotPropagateAndStillInvokesOthers()
    {
        var underlying = CreateUnderlyingFlow(out _);
        var webSocketFlow = new WebSocketFlow(underlying);
        var laterSubscriberInvoked = false;
        webSocketFlow.Closed += () => throw new InvalidOperationException("boom");
        webSocketFlow.Closed += () => laterSubscriberInvoked = true;

        webSocketFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(laterSubscriberInvoked).IsTrue();
        await Assert.That(webSocketFlow.IsClosed).IsTrue();
    }

    private static TrafficFlow CreateUnderlyingFlow(out Guid id)
    {
        id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "127.0.0.1:0", DateTimeOffset.UtcNow);
        return flow;
    }
}
