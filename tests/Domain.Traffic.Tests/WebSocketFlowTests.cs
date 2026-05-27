using System;
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

    private static TrafficFlow CreateUnderlyingFlow(out Guid id)
    {
        id = Guid.NewGuid();
        var flow = new TrafficFlow(id, "127.0.0.1:0", DateTimeOffset.UtcNow);
        return flow;
    }
}
