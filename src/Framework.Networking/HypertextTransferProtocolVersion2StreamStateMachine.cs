namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure state-transition table for the HTTP/2 stream state machine defined in RFC 7540 § 5.1.
///     Helpers are split by event type so each branch is small and unit-testable.
/// </summary>
public static class HypertextTransferProtocolVersion2StreamStateMachine
{
    /// <summary>
    ///     Computes the next state after the local endpoint receives a DATA frame.
    /// </summary>
    /// <param name="current">The current state of the stream.</param>
    /// <param name="hasEndStreamFlag">Whether the DATA frame had END_STREAM set.</param>
    /// <returns>The transition result; a protocol error is reported when the stream is not open to receive DATA.</returns>
    public static HypertextTransferProtocolVersion2StreamTransitionResult OnDataReceived(
        HypertextTransferProtocolVersion2StreamState current,
        bool hasEndStreamFlag)
    {
        if (current == HypertextTransferProtocolVersion2StreamState.Open && hasEndStreamFlag)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.Open)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Open, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.HalfClosedLocal && hasEndStreamFlag)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Closed, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.HalfClosedLocal)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, isProtocolError: false);
        }
        return new HypertextTransferProtocolVersion2StreamTransitionResult(current, isProtocolError: true);
    }

    /// <summary>
    ///     Computes the next state after the local endpoint receives a HEADERS frame.
    /// </summary>
    /// <param name="current">The current state of the stream.</param>
    /// <param name="hasEndStreamFlag">Whether the HEADERS frame had END_STREAM set.</param>
    /// <returns>The transition result; a protocol error is reported when the event is illegal in <paramref name="current" />.</returns>
    public static HypertextTransferProtocolVersion2StreamTransitionResult OnHeadersReceived(
        HypertextTransferProtocolVersion2StreamState current,
        bool hasEndStreamFlag)
    {
        if (current == HypertextTransferProtocolVersion2StreamState.Idle)
        {
            var openState = hasEndStreamFlag
                ? HypertextTransferProtocolVersion2StreamState.HalfClosedRemote
                : HypertextTransferProtocolVersion2StreamState.Open;
            return new HypertextTransferProtocolVersion2StreamTransitionResult(openState, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.ReservedRemote)
        {
            var nextState = hasEndStreamFlag
                ? HypertextTransferProtocolVersion2StreamState.Closed
                : HypertextTransferProtocolVersion2StreamState.HalfClosedLocal;
            return new HypertextTransferProtocolVersion2StreamTransitionResult(nextState, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.Open && hasEndStreamFlag)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.HalfClosedRemote, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.Open)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Open, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.HalfClosedLocal && hasEndStreamFlag)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Closed, isProtocolError: false);
        }
        if (current == HypertextTransferProtocolVersion2StreamState.HalfClosedLocal)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.HalfClosedLocal, isProtocolError: false);
        }
        return new HypertextTransferProtocolVersion2StreamTransitionResult(current, isProtocolError: true);
    }

    /// <summary>
    ///     Computes the next state after a PUSH_PROMISE frame reserves a remote stream.
    /// </summary>
    /// <param name="current">The current state — must be <c>Idle</c>.</param>
    /// <returns>The transition result.</returns>
    public static HypertextTransferProtocolVersion2StreamTransitionResult OnPushPromiseReceived(
        HypertextTransferProtocolVersion2StreamState current)
    {
        if (current == HypertextTransferProtocolVersion2StreamState.Idle)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.ReservedRemote, isProtocolError: false);
        }
        return new HypertextTransferProtocolVersion2StreamTransitionResult(current, isProtocolError: true);
    }

    /// <summary>
    ///     Computes the next state after RST_STREAM is sent or received for the stream.
    ///     RST_STREAM moves any non-idle stream straight to <c>Closed</c>; receiving it on an
    ///     <c>Idle</c> stream is itself a protocol error.
    /// </summary>
    /// <param name="current">The current state.</param>
    /// <returns>The transition result.</returns>
    public static HypertextTransferProtocolVersion2StreamTransitionResult OnResetReceived(
        HypertextTransferProtocolVersion2StreamState current)
    {
        if (current == HypertextTransferProtocolVersion2StreamState.Idle)
        {
            return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Idle, isProtocolError: true);
        }
        return new HypertextTransferProtocolVersion2StreamTransitionResult(HypertextTransferProtocolVersion2StreamState.Closed, isProtocolError: false);
    }
}
