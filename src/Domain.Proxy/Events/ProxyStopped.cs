using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server stops listening, either via an explicit stop or dispose.
/// </summary>
public sealed record ProxyStopped : IDomainEvent
{
    /// <summary>
    ///     Gets the UTC instant at which the proxy stopped.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyStopped" /> event.
    /// </summary>
    /// <param name="timestamp">The UTC instant at which the proxy stopped.</param>
    public ProxyStopped(DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
    }
}