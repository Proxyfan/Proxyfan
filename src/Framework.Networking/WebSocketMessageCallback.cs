using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Delegate invoked by <see cref="WebSocketRelay" /> for every fully-reassembled message.
///     Invoked synchronously from the relay loop so callers must keep work fast or marshal
///     to a background task.
/// </summary>
/// <param name="message">The captured WebSocket message.</param>
public delegate void WebSocketMessageCallback(WebSocketMessage message);
