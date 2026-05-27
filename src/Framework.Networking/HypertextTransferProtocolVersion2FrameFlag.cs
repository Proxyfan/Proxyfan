using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HTTP/2 frame flag bits as defined by RFC 7540 § 6. Several frame types reuse the same
///     bit position with different semantics (e.g. END_STREAM on DATA/HEADERS vs ACK on
///     PING/SETTINGS), so this enum represents the raw on-the-wire flag bits and consumers
///     interpret them through frame-type-aware helpers.
/// </summary>
[Flags]
public enum HypertextTransferProtocolVersion2FrameFlag
{
    /// <summary>
    ///     No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    ///     Bit 0x01: END_STREAM on DATA and HEADERS, ACK on PING and SETTINGS.
    /// </summary>
    EndStreamOrAcknowledge = 0x01,

    /// <summary>
    ///     Bit 0x04: END_HEADERS on HEADERS, PUSH_PROMISE and CONTINUATION.
    /// </summary>
    EndHeaders = 0x04,

    /// <summary>
    ///     Bit 0x08: PADDED on DATA, HEADERS and PUSH_PROMISE.
    /// </summary>
    Padded = 0x08,

    /// <summary>
    ///     Bit 0x20: PRIORITY on HEADERS.
    /// </summary>
    Priority = 0x20,
}
