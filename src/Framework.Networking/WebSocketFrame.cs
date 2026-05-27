using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parsed WebSocket frame as defined by RFC 6455 § 5.2.
/// </summary>
public sealed class WebSocketFrame
{
    /// <summary>
    ///     Gets a value indicating whether this is the final fragment of a message (FIN bit).
    /// </summary>
    public bool IsFinalFragment { get; }

    /// <summary>
    ///     Gets the frame's opcode (text/binary/continuation/control).
    /// </summary>
    public WebSocketOpcode Opcode { get; }

    /// <summary>
    ///     Gets the (already-unmasked) payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    ///     Gets the total bytes consumed from the source buffer to produce this frame
    ///     (header + payload). Callers should advance their cursor by this amount.
    /// </summary>
    public int TotalLength { get; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketFrame" />.
    /// </summary>
    /// <param name="isFinalFragment">Whether this is the final fragment of a message.</param>
    /// <param name="opcode">The frame opcode.</param>
    /// <param name="payload">The unmasked payload bytes.</param>
    /// <param name="totalLength">Total bytes consumed (header + payload).</param>
    public WebSocketFrame(bool isFinalFragment, WebSocketOpcode opcode, ReadOnlyMemory<byte> payload, int totalLength)
    {
        IsFinalFragment = isFinalFragment;
        Opcode = opcode;
        Payload = payload;
        TotalLength = totalLength;
    }
}
