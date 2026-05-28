using Proxyfan.Domain.Proxy;
using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Per-connection context for the TLS-intercepted HTTP loop. Bundles the connection,
///     the four pipes used by the loop, and the underlying TLS streams so the loop can hand
///     them off to a WebSocket tunnel without violating the analyzer's parameter limit
///     (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedLoopContext
{
    /// <summary>
    ///     Gets the TLS-terminated client stream used for raw byte tunneling.
    /// </summary>
    public required Stream ClientSecureStream { get; init; }

    /// <summary>
    ///     Gets the proxy connection that produced this exchange.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the four pipes used by the HTTP loop.
    /// </summary>
    public required TransportLayerSecurityInterceptionPipes Pipes { get; init; }

    /// <summary>
    ///     Gets the TLS-encrypted upstream stream used for raw byte tunneling.
    /// </summary>
    public required Stream ServerSecureStream { get; init; }
}
