using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2Stream" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2StreamTests
{
    /// <summary>
    ///     A new stream starts in <c>Idle</c> with default-sized windows.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultWindows_StartsIdle()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);

        await Assert.That(stream.StreamIdentifier).IsEqualTo((uint)1);
        await Assert.That(stream.State).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Idle);
        await Assert.That(stream.ReceiveWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);
        await Assert.That(stream.SendWindow.Available).IsEqualTo(HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize);
    }

    /// <summary>
    ///     Stream id 0 is reserved for connection control and cannot be used as a stream id.
    /// </summary>
    [Test]
    public async Task Constructor_StreamIdZero_Throws()
    {
        await Assert.That(() => new HypertextTransferProtocolVersion2Stream(0)).Throws<ArgumentException>();
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2Stream.AppendBody" /> accumulates payload
    ///     bytes from DATA frames.
    /// </summary>
    [Test]
    public async Task AppendBody_TwoChunks_ProducesConcatenation()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);
        byte[] first = [1, 2, 3];
        byte[] second = [4, 5];

        stream.AppendBody(first);
        stream.AppendBody(second);

        await Assert.That(stream.Body).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2Stream.AppendHeaders" /> accumulates header lists.
    /// </summary>
    [Test]
    public async Task AppendHeaders_TwoBatches_ProducesUnion()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);
        var first = new[] { new HypertextTransferProtocolVersion2HpackHeaderField(":method", "GET") };
        var second = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField(":path", "/"),
            new HypertextTransferProtocolVersion2HpackHeaderField(":scheme", "https"),
        };

        stream.AppendHeaders(first);
        stream.AppendHeaders(second);

        await Assert.That(stream.Headers.Count).IsEqualTo(3);
        await Assert.That(stream.Headers[0].Name).IsEqualTo(":method");
        await Assert.That(stream.Headers[2].Value).IsEqualTo("https");
    }

    /// <summary>
    ///     A successful HEADERS application updates the stream's state.
    /// </summary>
    [Test]
    public async Task ApplyHeadersReceived_IdleStream_TransitionsToOpen()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);

        var result = stream.ApplyHeadersReceived(hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(stream.State).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Open);
    }

    /// <summary>
    ///     A protocol-error HEADERS application leaves the stream's state unchanged.
    /// </summary>
    [Test]
    public async Task ApplyHeadersReceived_ClosedStream_LeavesStateUnchanged()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);
        stream.HasClosed();

        var result = stream.ApplyHeadersReceived(hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsTrue();
        await Assert.That(stream.State).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Closed);
    }

    /// <summary>
    ///     A DATA application after HEADERS half-closes the remote side on END_STREAM.
    /// </summary>
    [Test]
    public async Task ApplyDataReceived_OpenWithEndStream_HalfClosesRemotely()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);
        stream.ApplyHeadersReceived(hasEndStreamFlag: false);

        var result = stream.ApplyDataReceived(hasEndStreamFlag: true);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(stream.State).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote);
    }

    /// <summary>
    ///     PUSH_PROMISE on an idle stream transitions to ReservedRemote.
    /// </summary>
    [Test]
    public async Task ApplyPushPromiseReceived_Idle_TransitionsToReservedRemote()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(4);

        var result = stream.ApplyPushPromiseReceived();

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(stream.State).IsEqualTo(HypertextTransferProtocolVersion2StreamState.ReservedRemote);
    }

    /// <summary>
    ///     <see cref="HypertextTransferProtocolVersion2Stream.HasClosed" /> reports whether it
    ///     actually transitioned the state.
    /// </summary>
    [Test]
    public async Task HasClosed_AlreadyClosed_ReturnsFalse()
    {
        var stream = new HypertextTransferProtocolVersion2Stream(1);
        stream.HasClosed();

        var second = stream.HasClosed();

        await Assert.That(second).IsFalse();
    }
}
