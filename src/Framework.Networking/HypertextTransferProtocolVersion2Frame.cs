using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A parsed HTTP/2 frame: header plus payload bytes. The payload is owned by the parser's
///     caller — instances should not be retained beyond the lifetime of the underlying buffer
///     unless the caller copies the payload.
/// </summary>
public sealed class HypertextTransferProtocolVersion2Frame
{
    /// <summary>
    ///     Gets the parsed frame header.
    /// </summary>
    public HypertextTransferProtocolVersion2FrameHeader Header { get; }

    /// <summary>
    ///     Gets the raw payload bytes (length matches <see cref="HypertextTransferProtocolVersion2FrameHeader.Length" />).
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolVersion2Frame" />.
    /// </summary>
    /// <param name="header">The parsed header.</param>
    /// <param name="payload">The payload bytes.</param>
    public HypertextTransferProtocolVersion2Frame(
        HypertextTransferProtocolVersion2FrameHeader header,
        ReadOnlyMemory<byte> payload)
    {
        Header = header;
        Payload = payload;
    }
}
