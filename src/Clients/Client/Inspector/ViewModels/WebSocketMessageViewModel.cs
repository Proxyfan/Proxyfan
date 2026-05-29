using Proxyfan.Domain.Traffic;
using System.Globalization;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model wrapping a single <see cref="WebSocketMessage" /> for display in
///     the WebSocket inspector message list.
/// </summary>
public sealed class WebSocketMessageViewModel
{
    /// <summary>
    ///     Gets the direction indicator glyph: <c>↑</c> for outbound (client→server)
    ///     and <c>↓</c> for inbound (server→client).
    /// </summary>
    public string DirectionGlyph { get; }

    /// <summary>
    ///     Gets the wrapped domain message instance.
    /// </summary>
    public WebSocketMessage Message { get; }

    /// <summary>
    ///     Gets the human-readable opcode label (Text, Binary, Ping, Pong, Close).
    /// </summary>
    public string OpcodeText { get; }

    /// <summary>
    ///     Gets a one-line preview of the payload for the message list.
    /// </summary>
    public string PayloadPreview { get; }

    /// <summary>
    ///     Gets the payload size in bytes as a formatted string (e.g. "12 B", "3.4 KB").
    /// </summary>
    public string PayloadSizeText { get; }

    /// <summary>
    ///     Gets the wall-clock timestamp the message was captured, formatted as <c>HH:mm:ss.fff</c>.
    /// </summary>
    public string TimestampText { get; }

    /// <summary>
    ///     Initializes a new <see cref="WebSocketMessageViewModel" /> wrapping the
    ///     supplied domain message.
    /// </summary>
    /// <param name="message">The captured WebSocket message.</param>
    public WebSocketMessageViewModel(WebSocketMessage message)
    {
        Message = message;
        DirectionGlyph = message.Direction == WebSocketDirection.Outbound ? "↑" : "↓";
        OpcodeText = WebSocketOpcodeFormatter.FormatOpcode(message.Opcode);
        PayloadPreview = WebSocketPayloadFormatter.FormatPreview(message);
        PayloadSizeText = WebSocketByteSizeFormatter.Format(message.Payload.Length);
        TimestampText = message.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
