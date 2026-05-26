using System;

namespace Proxyfan.Domain.Proxy.Events;

/// <summary>
///     Published when the proxy server successfully starts listening on a port.
/// </summary>
public sealed record ProxyStarted : IDomainEvent
{
    /// <summary>
    ///     Gets the port the proxy is now listening on.
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    ///     Gets the UTC instant at which the proxy started.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyStarted" /> event.
    /// </summary>
    /// <param name="port">The port the proxy is now listening on.</param>
    /// <param name="timestamp">The UTC instant at which the proxy started.</param>
    public ProxyStarted(int port, DateTimeOffset timestamp)
    {
        Port = port;
        Timestamp = timestamp;
    }
}