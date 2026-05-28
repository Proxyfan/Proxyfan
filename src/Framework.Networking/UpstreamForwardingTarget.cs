namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the upstream connect target and rewritten request header bytes used by
///     <see cref="HypertextTransferProtocolForwarder" /> when sending a request upstream.
/// </summary>
public sealed class UpstreamForwardingTarget
{
    /// <summary>
    ///     Gets the rewritten request header bytes that must be written verbatim to the
    ///     upstream connection. When the upstream proxy is engaged these bytes contain an
    ///     absolute-form request line and the proxy authorization header; otherwise they
    ///     contain the origin-form headers.
    /// </summary>
    public required byte[] HeaderBytes { get; init; }

    /// <summary>
    ///     Gets the TCP endpoint that the proxy must connect to. This is either the origin
    ///     host parsed from the request, or the configured upstream proxy.
    /// </summary>
    public required ConnectTarget Target { get; init; }
}
