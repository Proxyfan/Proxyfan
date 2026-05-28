using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="HypertextTransferProtocolVersion2StreamStateMachine" /> covering the
///     RFC 7540 § 5.1 transition table.
/// </summary>
public sealed class HypertextTransferProtocolVersion2StreamStateMachineTests
{
    /// <summary>
    ///     HEADERS without END_STREAM opens an idle stream.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_IdleStreamWithoutEndStream_BecomesOpen()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.Idle, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Open);
    }

    /// <summary>
    ///     HEADERS with END_STREAM opens an idle stream and immediately half-closes it remotely.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_IdleStreamWithEndStream_BecomesHalfClosedRemote()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.Idle, hasEndStreamFlag: true);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote);
    }

    /// <summary>
    ///     HEADERS on a reserved-remote stream (server push response) becomes half-closed-local.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_ReservedRemoteWithoutEndStream_BecomesHalfClosedLocal()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.ReservedRemote, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal);
    }

    /// <summary>
    ///     HEADERS+END_STREAM on a reserved-remote stream closes it immediately.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_ReservedRemoteWithEndStream_BecomesClosed()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.ReservedRemote, hasEndStreamFlag: true);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Closed);
    }

    /// <summary>
    ///     HEADERS+END_STREAM on an open stream half-closes it remotely.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_OpenWithEndStream_BecomesHalfClosedRemote()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.Open, hasEndStreamFlag: true);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote);
    }

    /// <summary>
    ///     HEADERS+END_STREAM on a half-closed-local stream closes it.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_HalfClosedLocalWithEndStream_BecomesClosed()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, hasEndStreamFlag: true);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Closed);
    }

    /// <summary>
    ///     HEADERS on a closed stream is a protocol error.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_Closed_IsProtocolError()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.Closed, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsTrue();
    }

    /// <summary>
    ///     DATA on an open stream stays open.
    /// </summary>
    [Test]
    public async Task OnDataReceived_OpenWithoutEndStream_StaysOpen()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(HypertextTransferProtocolVersion2StreamState.Open, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Open);
    }

    /// <summary>
    ///     DATA+END_STREAM on an open stream half-closes it remotely.
    /// </summary>
    [Test]
    public async Task OnDataReceived_OpenWithEndStream_BecomesHalfClosedRemote()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(HypertextTransferProtocolVersion2StreamState.Open, hasEndStreamFlag: true);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote);
    }

    /// <summary>
    ///     DATA+END_STREAM on a half-closed-local stream closes it.
    /// </summary>
    [Test]
    public async Task OnDataReceived_HalfClosedLocalWithEndStream_BecomesClosed()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, hasEndStreamFlag: true);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Closed);
    }

    /// <summary>
    ///     DATA received on an idle stream is a protocol error per RFC 7540 § 5.1.
    /// </summary>
    [Test]
    public async Task OnDataReceived_Idle_IsProtocolError()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(HypertextTransferProtocolVersion2StreamState.Idle, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsTrue();
    }

    /// <summary>
    ///     RST_STREAM on any non-idle stream moves it directly to closed.
    /// </summary>
    [Test]
    [Arguments(HypertextTransferProtocolVersion2StreamState.Open)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.ReservedLocal)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.ReservedRemote)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.Closed)]
    public async Task OnResetReceived_NonIdleState_BecomesClosed(HypertextTransferProtocolVersion2StreamState initial)
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnResetReceived(initial);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Closed);
    }

    /// <summary>
    ///     RST_STREAM on an idle stream is a protocol error.
    /// </summary>
    [Test]
    public async Task OnResetReceived_Idle_IsProtocolError()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnResetReceived(HypertextTransferProtocolVersion2StreamState.Idle);

        await Assert.That(result.IsProtocolError).IsTrue();
    }

    /// <summary>
    ///     PUSH_PROMISE on an idle stream reserves it remotely.
    /// </summary>
    [Test]
    public async Task OnPushPromiseReceived_Idle_BecomesReservedRemote()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnPushPromiseReceived(HypertextTransferProtocolVersion2StreamState.Idle);

        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.ReservedRemote);
    }

    /// <summary>
    ///     PUSH_PROMISE on an already-open or closed stream is a protocol error.
    /// </summary>
    [Test]
    [Arguments(HypertextTransferProtocolVersion2StreamState.Open)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.Closed)]
    [Arguments(HypertextTransferProtocolVersion2StreamState.ReservedRemote)]
    public async Task OnPushPromiseReceived_NonIdleState_IsProtocolError(HypertextTransferProtocolVersion2StreamState initial)
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnPushPromiseReceived(initial);

        await Assert.That(result.IsProtocolError).IsTrue();
    }

    /// <summary>
    ///     DATA without END_STREAM on a half-closed-local stream keeps the half-closed state
    ///     (the stream remains receiving until END_STREAM arrives). Exercises the
    ///     HalfClosedLocal-without-END_STREAM branch in OnDataReceived.
    /// </summary>
    [Test]
    public async Task OnDataReceived_HalfClosedLocalWithoutEndStream_StaysHalfClosedLocal()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal);
    }

    /// <summary>
    ///     A trailing HEADERS frame without END_STREAM on an open stream is permitted by the
    ///     RFC 7540 transition table and leaves the state unchanged. Exercises the
    ///     Open-without-END_STREAM branch in OnHeadersReceived.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_OpenWithoutEndStream_StaysOpen()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.Open, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.Open);
    }

    /// <summary>
    ///     Trailing HEADERS without END_STREAM on a half-closed-local stream leaves the
    ///     half-closed-local state unchanged. Exercises the HalfClosedLocal-without-END_STREAM
    ///     branch in OnHeadersReceived.
    /// </summary>
    [Test]
    public async Task OnHeadersReceived_HalfClosedLocalWithoutEndStream_StaysHalfClosedLocal()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, hasEndStreamFlag: false);

        await Assert.That(result.IsProtocolError).IsFalse();
        await Assert.That(result.NextState).IsEqualTo(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal);
    }
}
