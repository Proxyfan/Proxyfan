namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Delegate raised when a new <see cref="WebSocketMessage" /> is appended to a
///     <see cref="WebSocketFlow" /> via <see cref="WebSocketFlow.RecordMessage" />.
///     The handler receives the message that was just recorded.
/// </summary>
/// <param name="message">The message that was appended to the flow.</param>
public delegate void WebSocketMessageRecordedHandler(WebSocketMessage message);
