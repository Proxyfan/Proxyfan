using System;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Traffic.Tests;

/// <summary>
///     Tests for <see cref="RemoteProcedureCallFlow" />.
/// </summary>
public sealed class RemoteProcedureCallFlowTests
{
    /// <summary>
    ///     A fresh flow is open and contains no messages.
    /// </summary>
    [Test]
    public async Task Constructor_FreshFlow_IsOpenWithNoMessages()
    {
        var flow = CreateUnderlyingFlow(out _);

        var rpcFlow = new RemoteProcedureCallFlow(flow);

        await Assert.That(rpcFlow.IsClosed).IsFalse();
        await Assert.That(rpcFlow.ClosedAt).IsNull();
        await Assert.That(rpcFlow.Messages.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     <see cref="RemoteProcedureCallFlow.Id" /> mirrors the underlying flow id.
    /// </summary>
    [Test]
    public async Task Id_MirrorsUnderlyingFlow_IsEqual()
    {
        var flow = CreateUnderlyingFlow(out var expectedId);
        var rpcFlow = new RemoteProcedureCallFlow(flow);

        await Assert.That(rpcFlow.Id).IsEqualTo(expectedId);
    }

    /// <summary>
    ///     <see cref="RemoteProcedureCallFlow.MarkClosed(DateTimeOffset)" /> only records the
    ///     first observed close timestamp.
    /// </summary>
    [Test]
    public async Task MarkClosed_CalledTwice_KeepsFirstTimestamp()
    {
        var flow = CreateUnderlyingFlow(out _);
        var rpcFlow = new RemoteProcedureCallFlow(flow);
        var firstClose = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var secondClose = new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero);

        rpcFlow.MarkClosed(firstClose);
        rpcFlow.MarkClosed(secondClose);

        await Assert.That(rpcFlow.IsClosed).IsTrue();
        await Assert.That(rpcFlow.ClosedAt).IsEqualTo(firstClose);
    }

    /// <summary>
    ///     <see cref="RemoteProcedureCallFlow.RecordMessage" /> appends messages in order
    ///     and preserves directions.
    /// </summary>
    [Test]
    public async Task RecordMessage_RequestThenResponse_PreservesOrderAndDirection()
    {
        var flow = CreateUnderlyingFlow(out _);
        var rpcFlow = new RemoteProcedureCallFlow(flow);
        var request = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01, 0x02, 0x03 },
            DateTimeOffset.UtcNow);
        var response = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            true,
            new byte[] { 0x04, 0x05 },
            DateTimeOffset.UtcNow);

        rpcFlow.RecordMessage(request);
        rpcFlow.RecordMessage(response);

        await Assert.That(rpcFlow.Messages.Count).IsEqualTo(2);
        await Assert.That(rpcFlow.Messages[0].Direction).IsEqualTo(RemoteProcedureCallDirection.Outbound);
        await Assert.That(rpcFlow.Messages[0].IsCompressed).IsFalse();
        await Assert.That(rpcFlow.Messages[0].Payload.Length).IsEqualTo(3);
        await Assert.That(rpcFlow.Messages[0].Timestamp).IsEqualTo(request.Timestamp);
        await Assert.That(rpcFlow.Messages[1].Direction).IsEqualTo(RemoteProcedureCallDirection.Inbound);
        await Assert.That(rpcFlow.Messages[1].IsCompressed).IsTrue();
    }

    /// <summary>
    ///     Appending beyond capacity evicts the oldest captured message and increments
    ///     <see cref="RemoteProcedureCallFlow.DroppedMessagesCount" />.
    /// </summary>
    [Test]
    public async Task RecordMessage_RemoteProcedureCallBeyondCapacity_EvictsOldest()
    {
        var flow = CreateUnderlyingFlow(out _);
        var budget = new StreamingCaptureBudget(1024);
        var rpcFlow = new RemoteProcedureCallFlow(flow, 2, budget);
        var first = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);
        var second = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            false,
            new byte[] { 0x02 },
            DateTimeOffset.UtcNow);
        var third = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x03 },
            DateTimeOffset.UtcNow);

        rpcFlow.RecordMessage(first);
        rpcFlow.RecordMessage(second);
        rpcFlow.RecordMessage(third);

        await Assert.That(rpcFlow.Messages.Count).IsEqualTo(2);
        await Assert.That(rpcFlow.Messages[0]).IsSameReferenceAs(second);
        await Assert.That(rpcFlow.Messages[1]).IsSameReferenceAs(third);
        await Assert.That(rpcFlow.DroppedMessagesCount).IsEqualTo(1);
    }

    /// <summary>
    ///     <see cref="RemoteProcedureCallFlow.Closed" /> fires exactly once on the first close
    ///     observation and never again.
    /// </summary>
    [Test]
    public async Task Closed_RaisedOnce_WhenMarkClosedCalledMultipleTimes()
    {
        var flow = CreateUnderlyingFlow(out _);
        var rpcFlow = new RemoteProcedureCallFlow(flow);
        var closeCount = 0;
        rpcFlow.Closed += () => closeCount++;

        rpcFlow.MarkClosed(DateTimeOffset.UtcNow);
        rpcFlow.MarkClosed(DateTimeOffset.UtcNow);
        rpcFlow.MarkClosed(DateTimeOffset.UtcNow);

        await Assert.That(closeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     <see cref="RemoteProcedureCallFlow.MessageRecorded" /> fires for every recorded message.
    /// </summary>
    [Test]
    public async Task MessageRecorded_RaisedForEveryRecordedMessage_ReceivesAll()
    {
        var flow = CreateUnderlyingFlow(out _);
        var rpcFlow = new RemoteProcedureCallFlow(flow);
        var observed = new System.Collections.Generic.List<RemoteProcedureCallCapturedMessage>();
        rpcFlow.MessageRecorded += observed.Add;
        var first = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Outbound,
            false,
            new byte[] { 0x01 },
            DateTimeOffset.UtcNow);
        var second = new RemoteProcedureCallCapturedMessage(
            RemoteProcedureCallDirection.Inbound,
            false,
            new byte[] { 0x02 },
            DateTimeOffset.UtcNow);

        rpcFlow.RecordMessage(first);
        rpcFlow.RecordMessage(second);

        await Assert.That(observed.Count).IsEqualTo(2);
        await Assert.That(observed[0]).IsSameReferenceAs(first);
        await Assert.That(observed[1]).IsSameReferenceAs(second);
    }

    private static TrafficFlow CreateUnderlyingFlow(out Guid id)
    {
        id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "127.0.0.1:0", DateTimeOffset.UtcNow);
        return flow;
    }
}
