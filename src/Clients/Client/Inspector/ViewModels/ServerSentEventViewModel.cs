using Proxyfan.Domain.Traffic;
using System.Globalization;

namespace Proxyfan.Client.Inspector.ViewModels;

/// <summary>
///     View model wrapping a single captured <see cref="ServerSentEvent" /> for display in
///     the SSE inspector message list.
/// </summary>
public sealed class ServerSentEventViewModel
{
    /// <summary>
    ///     Gets a single-line preview of the event data with newlines collapsed and overflow
    ///     truncated with an ellipsis (suitable for the table row column).
    /// </summary>
    public string DataPreview { get; }

    /// <summary>
    ///     Gets the event-type label shown in the type column ("(default)" when the SSE event
    ///     omitted an explicit <c>event:</c> field).
    /// </summary>
    public string EventTypeText { get; }

    /// <summary>
    ///     Gets the captured event identifier shown in the id column ("(none)" when absent).
    /// </summary>
    public string IdText { get; }

    /// <summary>
    ///     Gets the wrapped domain event instance.
    /// </summary>
    public ServerSentEvent ServerSentEvent { get; }

    /// <summary>
    ///     Gets the data size in bytes as a formatted string (e.g. "12 B", "1.5 KB").
    /// </summary>
    public string SizeText { get; }

    /// <summary>
    ///     Gets the wall-clock capture timestamp formatted as <c>HH:mm:ss.fff</c>.
    /// </summary>
    public string TimestampText { get; }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEventViewModel" /> wrapping the supplied
    ///     domain event.
    /// </summary>
    /// <param name="serverSentEvent">The captured SSE event.</param>
    public ServerSentEventViewModel(ServerSentEvent serverSentEvent)
    {
        ServerSentEvent = serverSentEvent;
        EventTypeText = string.IsNullOrEmpty(serverSentEvent.EventType) ? "(default)" : serverSentEvent.EventType;
        IdText = string.IsNullOrEmpty(serverSentEvent.Id) ? "(none)" : serverSentEvent.Id;
        DataPreview = ServerSentEventPayloadFormatter.FormatPreview(serverSentEvent);
        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(serverSentEvent.Data);
        SizeText = WebSocketByteSizeFormatter.Format(sizeBytes);
        TimestampText = serverSentEvent.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
