namespace Proxyfan.Framework.Networking;

/// <summary>
///     Strategy enum chosen by the TLS interceptor based on whether a host is on the SSL
///     proxying list. Encapsulating the synchronous decision in its own static helper makes
///     the branch testable in isolation rather than buried in an async state machine.
/// </summary>
public enum TransportLayerSecurityHandlingStrategy
{
    /// <summary>
    ///     Forward bytes blindly between client and server (host is NOT on the SSL proxying list).
    /// </summary>
    PassThroughTunnel = 0,

    /// <summary>
    ///     Terminate the client TLS, re-establish TLS to the server, and inspect plaintext
    ///     between (host IS on the SSL proxying list).
    /// </summary>
    InterceptAndInspect = 1,
}
