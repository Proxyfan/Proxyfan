using System;
using System.Net;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when a connection handler throws an unhandled exception while processing
///     an accepted connection. The connection is closed after this event is published.
/// </summary>
public sealed record ConnectionErrorOccurred : IDomainEvent
{
    /// <summary>
    ///     Gets the exception that caused the connection to fail.
    /// </summary>
    public Exception Exception { get; init; }

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
    /// <param name="exception">The exception that caused the connection to fail.</param>
    /// <param name="timestamp">The UTC instant at which the error occurred.</param>
    public ConnectionErrorOccurred(EndPoint remoteEndPoint, Exception exception, DateTimeOffset timestamp)
    {
        RemoteEndPoint = remoteEndPoint;
        Exception = exception;
        Timestamp = timestamp;
    }
}