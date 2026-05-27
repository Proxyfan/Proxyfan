namespace Proxyfan.Framework.Networking;

/// <summary>
///     Fully-specified parameters required to write an HTTP/2 frame header (RFC 7540 § 4.1).
///     Uses required-init properties so callers spell each field out at the call site, which
///     avoids the four-parameter limit of the writer's static methods.
/// </summary>
public readonly record struct HypertextTransferProtocolVersion2FrameDescriptor
{
    /// <summary>
    ///     Gets the flag bits.
    /// </summary>
    public required HypertextTransferProtocolVersion2FrameFlag Flags { get; init; }

    /// <summary>
    ///     Gets the payload length (must fit in 24 bits, 0 – 16 777 215).
    /// </summary>
    public required int PayloadLength { get; init; }

    /// <summary>
    ///     Gets the stream identifier (top reserved bit is cleared automatically).
    /// </summary>
    public required uint StreamIdentifier { get; init; }

    /// <summary>
    ///     Gets the frame type byte.
    /// </summary>
    public required HypertextTransferProtocolVersion2FrameType Type { get; init; }
}
