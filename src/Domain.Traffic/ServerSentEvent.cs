using System;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     A single Server-Sent Events event as defined by the HTML Living Standard SSE format
///     (https://html.spec.whatwg.org/multipage/server-sent-events.html). Multi-line data
///     fields are joined with a newline.
/// </summary>
public sealed class ServerSentEvent
{
    /// <summary>
    ///     Gets the event data (multiple data: lines joined by newline). Always non-null but
    ///     may be empty.
    /// </summary>
    public string Data { get; }

    /// <summary>
    ///     Gets the event type (the value of the last <c>event:</c> field), or <see langword="null" /> when absent.
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    ///     Gets the event id (the value of the last <c>id:</c> field), or <see langword="null" /> when absent.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    ///     Gets the retry interval in milliseconds (the value of the last <c>retry:</c> field that
    ///     contained a non-negative integer), or <see langword="null" /> when absent or malformed.
    /// </summary>
    public int? RetryMilliseconds { get; }

    /// <summary>
    ///     Gets the wall-clock timestamp at which the event was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Initializes a new <see cref="ServerSentEvent" />.
    /// </summary>
    /// <param name="data">The event data.</param>
    /// <param name="eventType">The event type, or null.</param>
    /// <param name="id">The event id, or null.</param>
    /// <param name="retryMilliseconds">The retry interval in milliseconds, or null.</param>
    /// <param name="timestamp">The capture timestamp.</param>
    public ServerSentEvent(
        string data,
        string? eventType,
        string? id,
        int? retryMilliseconds,
        DateTimeOffset timestamp)
    {
        Data = data;
        EventType = eventType;
        Id = id;
        RetryMilliseconds = retryMilliseconds;
        Timestamp = timestamp;
    }
}
