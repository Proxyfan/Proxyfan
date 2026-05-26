using System;
using System.Net.Sockets;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     The exception thrown when the proxy listener fails to bind to the configured port,
///     for example because the port is already in use or access is denied.
/// </summary>
public sealed class ProxyBindException : Exception
{
    /// <summary>
    ///     Gets the port number that could not be bound.
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Initializes a new <see cref="ProxyBindException" />.
    /// </summary>
    /// <param name="port">The port number that could not be bound.</param>
    /// <param name="innerException">The underlying <see cref="SocketException" /> that caused the failure.</param>
    public ProxyBindException(int port, SocketException innerException)
        : base($"Failed to bind proxy listener to port {port}: {innerException.Message}", innerException)
    {
        Port = port;
    }
}