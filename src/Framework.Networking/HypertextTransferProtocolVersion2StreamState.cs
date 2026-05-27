namespace Proxyfan.Framework.Networking;

/// <summary>
///     HTTP/2 stream state machine values per RFC 7540 § 5.1.
/// </summary>
public enum HypertextTransferProtocolVersion2StreamState
{
    /// <summary>
    ///     The initial state. All streams start in <see cref="Idle" />.
    /// </summary>
    Idle = 0,

    /// <summary>
    ///     The stream has been reserved by a local PUSH_PROMISE. The endpoint may send
    ///     HEADERS on this stream to begin its response.
    /// </summary>
    ReservedLocal = 1,

    /// <summary>
    ///     The stream has been reserved by a remote PUSH_PROMISE. The endpoint will receive
    ///     HEADERS on this stream when the peer begins its response.
    /// </summary>
    ReservedRemote = 2,

    /// <summary>
    ///     The stream is open: both endpoints may send frames of any type.
    /// </summary>
    Open = 3,

    /// <summary>
    ///     The local endpoint has sent END_STREAM but may still receive frames.
    /// </summary>
    HalfClosedLocal = 4,

    /// <summary>
    ///     The remote endpoint has sent END_STREAM but may still send frames.
    /// </summary>
    HalfClosedRemote = 5,

    /// <summary>
    ///     The stream is closed. No frames may be sent or received.
    /// </summary>
    Closed = 6,
}
