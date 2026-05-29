namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a <see cref="WebSocketFlow" /> is marked closed via
///     <see cref="WebSocketFlow.MarkClosed" />. Fires at most once per flow.
/// </summary>
public delegate void WebSocketFlowClosedHandler();
