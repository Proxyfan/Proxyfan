using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents the parsed target of an HTTP CONNECT request, consisting of a hostname
///     (or IP address) and a TCP port number.
/// </summary>
public sealed class ConnectTarget
{
    /// <summary>
    ///     Gets the target hostname or IP address extracted from the CONNECT request line.
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Gets the TCP port number extracted from the CONNECT request line.
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Initializes a new <see cref="ConnectTarget" /> with the specified host and port.
    /// </summary>
    /// <param name="host">The target hostname or IP address.</param>
    /// <param name="port">The target TCP port number.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="host" /> is null or white-space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="port" /> is not in the range 1–65535.
    /// </exception>
    public ConnectTarget(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host must not be null or white-space.", nameof(host));
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be in the range 1–65535.");
        }

        Host = host;
        Port = port;
    }
}
