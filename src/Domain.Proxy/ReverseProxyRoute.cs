using System;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Immutable description of a reverse proxy route: a listen port on the local host that
///     forwards incoming TCP connections to a configured backend host and port.
/// </summary>
public sealed class ReverseProxyRoute
{
    /// <summary>
    ///     Gets the backend host name to forward connections to.
    /// </summary>
    public string BackendHost { get; }

    /// <summary>
    ///     Gets the backend TCP port to forward connections to.
    /// </summary>
    public int BackendPort { get; }

    /// <summary>
    ///     Gets the unique route identifier (used in UI/state tracking).
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Gets the local TCP port the route binds to and accepts connections on.
    /// </summary>
    public int ListenPort { get; }

    /// <summary>
    ///     Gets the human-readable route name (shown in UI).
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the TLS mode used when handling client traffic. Currently only
    ///     <see cref="ReverseProxyTransportLayerSecurityMode.None" /> and
    ///     <see cref="ReverseProxyTransportLayerSecurityMode.Passthrough" /> are honored by the
    ///     engine; <see cref="ReverseProxyTransportLayerSecurityMode.Terminate" /> requires the
    ///     route to be paired with a server certificate (out of scope for the initial release).
    /// </summary>
    public ReverseProxyTransportLayerSecurityMode TransportLayerSecurityMode { get; }

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxyRoute" />.
    /// </summary>
    /// <param name="identifier">The unique route identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="listenPort">The local listen port (1..65535).</param>
    /// <param name="backendHost">The backend host name.</param>
    /// <param name="backendPort">The backend TCP port (1..65535).</param>
    /// <param name="transportLayerSecurityMode">The TLS handling mode.</param>
    /// <exception cref="ArgumentException">Thrown when host or name is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when ports are out of range.</exception>
    public ReverseProxyRoute(
        string identifier,
        string name,
        int listenPort,
        string backendHost,
        int backendPort,
        ReverseProxyTransportLayerSecurityMode transportLayerSecurityMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(backendHost);
        if (listenPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(listenPort));
        }

        if (backendPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(backendPort));
        }

        Identifier = identifier;
        Name = name;
        ListenPort = listenPort;
        BackendHost = backendHost;
        BackendPort = backendPort;
        TransportLayerSecurityMode = transportLayerSecurityMode;
    }
}
