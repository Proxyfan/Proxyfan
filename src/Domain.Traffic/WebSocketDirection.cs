namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Direction of a WebSocket message in a captured flow.
/// </summary>
public enum WebSocketDirection
{
    /// <summary>
    ///     Client-to-server message (always masked per RFC 6455).
    /// </summary>
    Outbound = 0,

    /// <summary>
    ///     Server-to-client message (never masked per RFC 6455).
    /// </summary>
    Inbound = 1,
}
