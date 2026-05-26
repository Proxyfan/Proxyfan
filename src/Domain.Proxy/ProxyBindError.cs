using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Error raised when the proxy listener fails to bind to the configured port.
/// </summary>
public sealed record ProxyBindError : ProxyError
{
    /// <summary>
    ///     Gets the port number that could not be bound.
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyBindError" />.
    /// </summary>
    /// <param name="port">The port number that could not be bound.</param>
    /// <param name="innerException">The underlying bind exception.</param>
    public ProxyBindError(int port, Exception innerException)
        : base(
            "PROXY_BIND_FAILED",
            $"Failed to bind proxy listener to port {port}: {innerException.Message}",
            innerException)
    {
        Port = port;
    }
}