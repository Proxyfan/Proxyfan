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
        await Assert.That(rpcFlow.Messages[1].Direction).IsEqualTo(RemoteProcedureCallDirection.Inbound);
        await Assert.That(rpcFlow.Messages[1].IsCompressed).IsTrue();
    }

    private static TrafficFlow CreateUnderlyingFlow(out Guid id)
    {
        id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "127.0.0.1:0", DateTimeOffset.UtcNow);
        return flow;
    }
}
