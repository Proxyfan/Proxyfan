namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     Direction filter applied to the WebSocket message list. Defaults to
///     <see cref="All" /> which keeps every captured message visible.
/// </summary>
public enum WebSocketDirectionFilter
{
    /// <summary>
    ///     No direction filter — both outbound and inbound messages are shown.
    /// </summary>
    All = 0,

    /// <summary>
    ///     Only outbound messages (client → server) are shown.
    /// </summary>
    Outbound = 1,

    /// <summary>
    ///     Only inbound messages (server → client) are shown.
    /// </summary>
    Inbound = 2,
}
