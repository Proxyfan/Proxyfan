namespace Proxyfan.Domain.Proxy;

/// <summary>
///     TLS handling modes for a reverse proxy route.
/// </summary>
public enum ReverseProxyTransportLayerSecurityMode
{
    /// <summary>
    ///     No TLS — accept plaintext from the client and forward plaintext to the backend.
    /// </summary>
    None = 0,

    /// <summary>
    ///     TLS passthrough — forward bytes byte-for-byte; client and backend negotiate TLS
    ///     directly without inspection.
    /// </summary>
    Passthrough = 1,

    /// <summary>
    ///     TLS termination — decrypt client TLS at the route, then optionally re-encrypt to the
    ///     backend. Requires a server certificate (handled out of band).
    /// </summary>
    Terminate = 2,
}
