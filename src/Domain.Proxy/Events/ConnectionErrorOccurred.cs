using System;
using System.Net;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when a connection handler throws an unhandled exception while processing
///     an accepted connection. The connection is closed after this event is published.
///     The event carries a typed, redacted <see cref="ProxyError" /> rather than the raw
///     exception so that exception messages and stack traces (which may contain hostnames,
///     request targets, local paths, or other diagnostic details) do not leak across the
///     domain event bus. The raw exception is kept only on the local logging path where
///     redaction policy is enforced.
/// </summary>
public sealed record ConnectionErrorOccurred : IDomainEvent
{
    /// <summary>
    ///     Gets the typed, redacted domain error describing the failure.
    /// </summary>
    public ProxyError Error { get; init; }

    /// <summary>
    ///     Gets the remote endpoint of the client whose connection failed.
    /// </summary>
    public EndPoint RemoteEndPoint { get; init; }

    /// <summary>
    ///     Gets the UTC instant at which the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ConnectionErrorOccurred" /> event.
    /// </summary>
    /// <param name="remoteEndPoint">The remote endpoint of the client whose connection failed.</param>
    /// <param name="error">The typed, redacted domain error describing the failure.</param>
    /// <param name="timestamp">The UTC instant at which the error occurred.</param>
    public ConnectionErrorOccurred(EndPoint remoteEndPoint, ProxyError error, DateTimeOffset timestamp)
    {
        RemoteEndPoint = remoteEndPoint;
        Error = error;
        Timestamp = timestamp;
    }
}
