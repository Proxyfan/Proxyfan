using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents a single, fully-reassembled WebSocket message captured by the proxy.
///     Continuation frames are reassembled into the final payload before construction.
/// </summary>
public sealed class WebSocketMessage
{
    /// <summary>
    ///     Gets the direction this message travelled.
    /// </summary>
    public WebSocketDirection Direction { get; }

    /// <summary>
    ///     Gets the WebSocket opcode for the assembled message (Text or Binary for data
    ///     messages; Ping/Pong/Close for control frames).
    /// </summary>
    public WebSocketOpcode Opcode { get; }

    /// <summary>
    ///     Gets the message payload bytes (already unmasked when applicable).
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    ///     Gets the wall-clock timestamp the message was completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketMessage" /> instance.
    /// </summary>
    /// <param name="direction">The travel direction.</param>
    /// <param name="opcode">The WebSocket opcode.</param>
    /// <param name="payload">The reassembled payload bytes (unmasked).</param>
    /// <param name="timestamp">The timestamp the message was captured.</param>
    public WebSocketMessage(
        WebSocketDirection direction,
        WebSocketOpcode opcode,
        ReadOnlyMemory<byte> payload,
        DateTimeOffset timestamp)
    {
        Direction = direction;
        Opcode = opcode;
        Payload = payload;
        Timestamp = timestamp;
    }
}
