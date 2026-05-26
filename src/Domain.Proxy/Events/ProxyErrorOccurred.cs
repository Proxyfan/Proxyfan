using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server encounters an error during a lifecycle operation
///     (start, stop, or restart).
/// </summary>
public sealed record ProxyErrorOccurred : IDomainEvent
{
    /// <summary>
    ///     Gets the domain error describing the failure.
    /// </summary>
    public ProxyError Error { get; init; }

    /// <summary>
    ///     Gets the UTC instant at which the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyErrorOccurred" /> event.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <param name="timestamp">The UTC instant at which the error occurred.</param>
    public ProxyErrorOccurred(ProxyError error, DateTimeOffset timestamp)
    {
        Error = error;
        Timestamp = timestamp;
    }
}