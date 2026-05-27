using System;
using System.Buffers;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A live HTTP/2 stream — owns its state, send/receive flow-control windows, accumulated
///     header list, and the streaming body buffer that DATA frames append into. Stream
///     instances are owned by an <see cref="HypertextTransferProtocolVersion2StreamRegistry" />
///     for a particular connection and are NOT thread-safe; the registry serializes access by
///     stream identifier.
/// </summary>
public sealed class HypertextTransferProtocolVersion2Stream
{
    private readonly ArrayBufferWriter<byte> _bodyBuffer;
    private readonly List<HypertextTransferProtocolVersion2HpackHeaderField> _headers;

    /// <summary>
    ///     Gets the accumulated body bytes appended by DATA frames so far.
    /// </summary>
    public byte[] Body => _bodyBuffer.WrittenSpan.ToArray();

    /// <summary>
    ///     Gets the decoded header list received so far on this stream.
    /// </summary>
    public IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> Headers => _headers;

    /// <summary>
    ///     Gets the receive flow-control window for incoming DATA on this stream — DATA bytes
    ///     consume it, WINDOW_UPDATE frames sent to the peer replenish it.
    /// </summary>
    public HypertextTransferProtocolVersion2FlowControlWindow ReceiveWindow { get; }

    /// <summary>
    ///     Gets the send flow-control window for outgoing DATA on this stream — DATA bytes
    ///     transmitted consume it, WINDOW_UPDATE frames received from the peer replenish it.
    /// </summary>
    public HypertextTransferProtocolVersion2FlowControlWindow SendWindow { get; }

    /// <summary>
    ///     Gets the current state of this stream.
    /// </summary>
    public HypertextTransferProtocolVersion2StreamState State { get; private set; }

    /// <summary>
    ///     Gets the stream identifier (odd numbers are client-initiated; even numbers are
    ///     server-initiated for server push). Stream id 0 is reserved for connection control.
    /// </summary>
    public uint StreamIdentifier { get; }

    /// <summary>
    ///     Initializes a new stream in the <see cref="HypertextTransferProtocolVersion2StreamState.Idle" /> state
    ///     with default-sized send and receive windows.
    /// </summary>
    /// <param name="streamIdentifier">The 31-bit stream identifier.</param>
    public HypertextTransferProtocolVersion2Stream(uint streamIdentifier)
        : this(streamIdentifier, HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize, HypertextTransferProtocolVersion2FlowControlWindow.DefaultInitialSize)
    {
    }

    /// <summary>
    ///     Initializes a new stream with explicit initial window sizes (typically derived from
    ///     SETTINGS negotiation).
    /// </summary>
    /// <param name="streamIdentifier">The 31-bit stream identifier.</param>
    /// <param name="initialReceiveWindowSize">Initial receive window size.</param>
    /// <param name="initialSendWindowSize">Initial send window size.</param>
    public HypertextTransferProtocolVersion2Stream(uint streamIdentifier, int initialReceiveWindowSize, int initialSendWindowSize)
    {
        if (streamIdentifier == 0)
        {
            throw new ArgumentException("Stream identifier 0 is reserved for connection control.", nameof(streamIdentifier));
        }
        StreamIdentifier = streamIdentifier;
        State = HypertextTransferProtocolVersion2StreamState.Idle;
        var receiveWindow = new HypertextTransferProtocolVersion2FlowControlWindow(initialReceiveWindowSize);
        ReceiveWindow = receiveWindow;
        var sendWindow = new HypertextTransferProtocolVersion2FlowControlWindow(initialSendWindowSize);
        SendWindow = sendWindow;
        var bodyBuffer = new ArrayBufferWriter<byte>();
        _bodyBuffer = bodyBuffer;
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>();
        _headers = headers;
    }

    /// <summary>
    ///     Appends <paramref name="data" /> to the body buffer (called when a DATA frame is
    ///     received for this stream).
    /// </summary>
    /// <param name="data">The DATA payload bytes (after padding removal).</param>
    public void AppendBody(ReadOnlySpan<byte> data)
    {
        _bodyBuffer.Write(data);
    }

    /// <summary>
    ///     Appends <paramref name="fields" /> to the accumulated header list (called when a
    ///     HEADERS or CONTINUATION block is fully decoded).
    /// </summary>
    /// <param name="fields">The decoded headers to append.</param>
    public void AppendHeaders(IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> fields)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            _headers.Add(fields[index]);
        }
    }

    /// <summary>
    ///     Applies a DATA frame to this stream, updating <see cref="State" /> according to
    ///     RFC 7540 § 5.1.
    /// </summary>
    /// <param name="hasEndStreamFlag">Whether the DATA frame had END_STREAM set.</param>
    /// <returns>The state transition result for inspection by the caller.</returns>
    public HypertextTransferProtocolVersion2StreamTransitionResult ApplyDataReceived(bool hasEndStreamFlag)
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnDataReceived(State, hasEndStreamFlag);
        if (!result.IsProtocolError)
        {
            State = result.NextState;
        }
        return result;
    }

    /// <summary>
    ///     Applies a HEADERS frame to this stream, updating <see cref="State" /> according to
    ///     RFC 7540 § 5.1.
    /// </summary>
    /// <param name="hasEndStreamFlag">Whether the HEADERS frame had END_STREAM set.</param>
    /// <returns>The state transition result for inspection by the caller.</returns>
    public HypertextTransferProtocolVersion2StreamTransitionResult ApplyHeadersReceived(bool hasEndStreamFlag)
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnHeadersReceived(State, hasEndStreamFlag);
        if (!result.IsProtocolError)
        {
            State = result.NextState;
        }
        return result;
    }

    /// <summary>
    ///     Applies a PUSH_PROMISE frame to this stream, transitioning it from <c>Idle</c> to
    ///     <c>ReservedRemote</c>.
    /// </summary>
    /// <returns>The state transition result.</returns>
    public HypertextTransferProtocolVersion2StreamTransitionResult ApplyPushPromiseReceived()
    {
        var result = HypertextTransferProtocolVersion2StreamStateMachine.OnPushPromiseReceived(State);
        if (!result.IsProtocolError)
        {
            State = result.NextState;
        }
        return result;
    }

    /// <summary>
    ///     Forces the stream into <see cref="HypertextTransferProtocolVersion2StreamState.Closed" />,
    ///     typically in response to RST_STREAM. Returns whether the stream actually transitioned.
    /// </summary>
    /// <returns><c>true</c> when the stream transitioned to <c>Closed</c>; <c>false</c> when it was already <c>Closed</c>.</returns>
    public bool HasClosed()
    {
        if (State == HypertextTransferProtocolVersion2StreamState.Closed)
        {
            return false;
        }
        State = HypertextTransferProtocolVersion2StreamState.Closed;
        return true;
    }
}
