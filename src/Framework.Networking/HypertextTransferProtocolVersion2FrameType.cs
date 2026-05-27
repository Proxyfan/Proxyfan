namespace Proxyfan.Framework.Networking;

/// <summary>
///     HTTP/2 frame types as defined by RFC 7540 § 11.2. Unknown frame types must be
///     ignored per the specification.
/// </summary>
public enum HypertextTransferProtocolVersion2FrameType
{
    /// <summary>
    ///     DATA frames carry arbitrary, variable-length sequences of octets.
    /// </summary>
    Data = 0x0,

    /// <summary>
    ///     HEADERS frame opens a stream and additionally carries a header block fragment.
    /// </summary>
    Headers = 0x1,

    /// <summary>
    ///     PRIORITY frame signals the sender-advised priority of a stream.
    /// </summary>
    Priority = 0x2,

    /// <summary>
    ///     RST_STREAM frame allows for immediate termination of a stream.
    /// </summary>
    ResetStream = 0x3,

    /// <summary>
    ///     SETTINGS frame conveys configuration parameters that affect endpoint communication.
    /// </summary>
    Settings = 0x4,

    /// <summary>
    ///     PUSH_PROMISE frame notifies the peer of a stream the sender intends to initiate.
    /// </summary>
    PushPromise = 0x5,

    /// <summary>
    ///     PING frame is a mechanism for measuring round-trip time and a basic liveness check.
    /// </summary>
    Ping = 0x6,

    /// <summary>
    ///     GOAWAY frame initiates shutdown of a connection or signals serious error conditions.
    /// </summary>
    GoAway = 0x7,

    /// <summary>
    ///     WINDOW_UPDATE frame is used to implement flow control.
    /// </summary>
    WindowUpdate = 0x8,

    /// <summary>
    ///     CONTINUATION frame is used to continue a sequence of header block fragments.
    /// </summary>
    Continuation = 0x9,
}
