using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ServerSentEventsRelay" /> verifying byte-for-byte forwarding and
///     event parsing.
/// </summary>
public sealed class ServerSentEventsRelayTests
{
    /// <summary>
    ///     Verifies that a single complete event is forwarded byte-for-byte and surfaced via
    ///     the callback.
    /// </summary>
    [Test]
    public async Task RelayAsync_SingleEvent_ForwardsAndCaptures()
    {
        var captured = new List<ServerSentEvent>();
        var relay = new ServerSentEventsRelay(captured.Add, TimeProvider.System);
        var payload = Encoding.UTF8.GetBytes("data: hello\n\n");
        using var source = new MemoryStream(payload);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured.Count).IsEqualTo(1);
        await Assert.That(captured[0].Data).IsEqualTo("hello");
        await Assert.That(destination.ToArray()).IsEquivalentTo(payload);
    }

    /// <summary>
    ///     Verifies that an event with id and event type is parsed.
    /// </summary>
    [Test]
    public async Task RelayAsync_EventWithIdAndType_PopulatesFields()
    {
        var captured = new List<ServerSentEvent>();
        var relay = new ServerSentEventsRelay(captured.Add, TimeProvider.System);
        var payload = Encoding.UTF8.GetBytes("id: 7\nevent: ping\ndata: payload\n\n");
        using var source = new MemoryStream(payload);
        using var destination = new MemoryStream();

        await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(captured[0].Id).IsEqualTo("7");
        await Assert.That(captured[0].EventType).IsEqualTo("ping");
        await Assert.That(captured[0].Data).IsEqualTo("payload");
    }

    /// <summary>
    ///     Verifies that two events in one chunk are both captured.
    /// </summary>
    [Test]
    public async Task RelayAsync_TwoEvents_CapturesBoth()
    {
        var captured = new List<ServerSentEvent>();
        var relay = new ServerSentEventsRelay(captured.Add, TimeProvider.System);
        var payload = Encoding.UTF8.GetBytes("data: first\n\ndata: second\n\n");
        using var source = new MemoryStream(payload);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(captured[0].Data).IsEqualTo("first");
        await Assert.That(captured[1].Data).IsEqualTo("second");
    }

    /// <summary>
    ///     Verifies that a single event split across two reads is still captured exactly once.
    /// </summary>
    [Test]
    public async Task RelayAsync_PartialReadsAcrossEventBoundary_CapturesOnce()
    {
        var captured = new List<ServerSentEvent>();
        var relay = new ServerSentEventsRelay(captured.Add, TimeProvider.System);
        var first = Encoding.UTF8.GetBytes("data: hel");
        var second = Encoding.UTF8.GetBytes("lo\n\n");
        using var source = new ChunkedStream(first, second);
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(captured[0].Data).IsEqualTo("hello");
        var combinedExpected = new byte[first.Length + second.Length];
        first.CopyTo(combinedExpected, 0);
        second.CopyTo(combinedExpected, first.Length);
        await Assert.That(destination.ToArray()).IsEquivalentTo(combinedExpected);
    }

    /// <summary>
    ///     Verifies the relay returns zero when the source produces no bytes.
    /// </summary>
    [Test]
    public async Task RelayAsync_EmptySource_ReturnsZero()
    {
        var captured = new List<ServerSentEvent>();
        var relay = new ServerSentEventsRelay(captured.Add, TimeProvider.System);
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        var count = await relay.RelayAsync(source, destination, CancellationToken.None);

        await Assert.That(count).IsEqualTo(0);
        await Assert.That(captured.Count).IsEqualTo(0);
        await Assert.That(destination.Length).IsEqualTo(0);
    }
}
