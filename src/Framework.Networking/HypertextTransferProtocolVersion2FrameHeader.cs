namespace Proxyfan.Framework.Networking;

/// <summary>
///     A parsed 9-octet HTTP/2 frame header (RFC 7540 § 4.1).
/// </summary>
public sealed class HypertextTransferProtocolVersion2FrameHeader
{
    /// <summary>
    ///     Gets the raw 8-bit flags byte. Interpretation is frame-type specific.
    /// </summary>
    public HypertextTransferProtocolVersion2FrameFlag Flags { get; }

    /// <summary>
    ///     Gets a value indicating whether <see cref="RawType" /> matches a known frame
    ///     type defined in RFC 7540. Unknown frame types must still be skipped over per the
    ///     specification.
    /// </summary>
    public bool IsKnownType => RawType <= 0x09;

    /// <summary>
    ///     Gets the declared payload length (24-bit unsigned, 0 – 16 777 215). Receivers
    ///     must reject lengths greater than SETTINGS_MAX_FRAME_SIZE.
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     Gets the raw 8-bit frame type byte. Prefer this over <see cref="Type" /> when
    ///     handling unknown frame types (the property returns the same numeric value cast
    ///     from <see cref="RawType" />).
    /// </summary>
    public byte RawType { get; }

    /// <summary>
    ///     Gets the stream identifier (31-bit unsigned). Stream id 0 refers to the whole
    ///     connection; stream id 0 frames include SETTINGS, PING, GOAWAY and connection-level
    ///     WINDOW_UPDATE.
    /// </summary>
    public uint StreamIdentifier { get; }

    /// <summary>
    ///     Gets the frame type when <see cref="IsKnownType" /> is true; otherwise returns
    ///     the value cast from the raw byte (which will not match any defined enum member).
    /// </summary>
    public HypertextTransferProtocolVersion2FrameType Type => (HypertextTransferProtocolVersion2FrameType)RawType;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolVersion2FrameHeader" />.
    /// </summary>
    /// <param name="length">The payload length.</param>
    /// <param name="rawType">The raw 8-bit frame type byte.</param>
    /// <param name="flags">The raw flags byte.</param>
    /// <param name="streamIdentifier">The stream identifier (top bit always cleared).</param>
    public HypertextTransferProtocolVersion2FrameHeader(
        int length,
        byte rawType,
        HypertextTransferProtocolVersion2FrameFlag flags,
        uint streamIdentifier)
    {
        Length = length;
        RawType = rawType;
        Flags = flags;
        StreamIdentifier = streamIdentifier;
    }
}
