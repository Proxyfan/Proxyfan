using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;
using System.Text;

namespace Proxyfan.Client.Inspector;

/// <summary>
///     Static helpers that format <see cref="ServerSentEvent" /> payloads for the SSE
///     inspector tab. Provides both a single-line preview (used in the row list) and a
///     full-detail rendering (used in the detail panel for the selected event).
/// </summary>
public static class ServerSentEventPayloadFormatter
{
    private const int PreviewLengthLimit = 120;

    /// <summary>
    ///     Returns a verbose multi-line rendering of <paramref name="serverSentEvent" /> showing
    ///     the type, id, retry hint, capture timestamp, and the full data field.
    /// </summary>
    /// <param name="serverSentEvent">The SSE event to render.</param>
    /// <returns>A multi-line human-readable rendering.</returns>
    public static string FormatFull(ServerSentEvent serverSentEvent)
    {
        var builder = new StringBuilder();
        builder.Append("Captured: ");
        builder.AppendLine(serverSentEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        builder.Append("Event   : ");
        builder.AppendLine(string.IsNullOrEmpty(serverSentEvent.EventType) ? "(default)" : serverSentEvent.EventType);
        builder.Append("Id      : ");
        builder.AppendLine(string.IsNullOrEmpty(serverSentEvent.Id) ? "(none)" : serverSentEvent.Id);
        if (serverSentEvent.RetryMilliseconds.HasValue)
        {
            builder.Append("Retry   : ");
            builder.AppendLine(serverSentEvent.RetryMilliseconds.Value.ToString(CultureInfo.InvariantCulture) + " ms");
        }

        builder.AppendLine();
        builder.AppendLine(serverSentEvent.Data);
        var result = builder.ToString();
        return result;
    }

    /// <summary>
    ///     Returns a single-line preview of the SSE event data field with newlines collapsed
    ///     into the literal sequence " ↵ " and the result truncated to a fixed character
    ///     length with an ellipsis when needed.
    /// </summary>
    /// <param name="serverSentEvent">The SSE event whose data to preview.</param>
    /// <returns>The single-line preview string.</returns>
    public static string FormatPreview(ServerSentEvent serverSentEvent)
    {
        var data = serverSentEvent.Data ?? string.Empty;
        var collapsed = data.Replace("\r\n", " ↵ ", StringComparison.Ordinal).Replace("\n", " ↵ ", StringComparison.Ordinal);

        if (collapsed.Length <= PreviewLengthLimit)
        {
            return collapsed;
        }

        var truncated = string.Concat(collapsed.AsSpan(0, PreviewLengthLimit), "…");
        return truncated;
    }
}
