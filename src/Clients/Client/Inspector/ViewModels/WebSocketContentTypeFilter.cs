namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     Content-type filter applied to the WebSocket message list. Defaults to
///     <see cref="All" /> which keeps every captured message visible.
/// </summary>
public enum WebSocketContentTypeFilter
{
    /// <summary>
    ///     No content-type filter — all opcodes are shown.
    /// </summary>
    All = 0,

    /// <summary>
    ///     Only text messages (opcode <c>Text</c>) are shown.
    /// </summary>
    Text = 1,

    /// <summary>
    ///     Only binary messages (opcode <c>Binary</c>) are shown.
    /// </summary>
    Binary = 2,

    /// <summary>
    ///     Only control messages (<c>Ping</c>, <c>Pong</c>, <c>Close</c>) are shown.
    /// </summary>
    Control = 3,
}
