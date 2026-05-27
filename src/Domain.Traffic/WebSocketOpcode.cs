namespace Proxyfan.Domain.Traffic;

/// <summary>
///     WebSocket frame opcodes as defined by RFC 6455 § 5.2.
/// </summary>
public enum WebSocketOpcode
{
    /// <summary>
    ///     Continuation frame (RFC 6455 § 5.2 opcode 0x0).
    /// </summary>
    Continuation = 0x0,

    /// <summary>
    ///     Text frame carrying UTF-8 encoded data (RFC 6455 § 5.2 opcode 0x1).
    /// </summary>
    Text = 0x1,

    /// <summary>
    ///     Binary frame carrying arbitrary bytes (RFC 6455 § 5.2 opcode 0x2).
    /// </summary>
    Binary = 0x2,

    /// <summary>
    ///     Connection-close control frame (RFC 6455 § 5.5.1 opcode 0x8).
    /// </summary>
    Close = 0x8,

    /// <summary>
    ///     Ping control frame (RFC 6455 § 5.5.2 opcode 0x9).
    /// </summary>
    Ping = 0x9,

    /// <summary>
    ///     Pong control frame (RFC 6455 § 5.5.3 opcode 0xA).
    /// </summary>
    Pong = 0xA,
}
