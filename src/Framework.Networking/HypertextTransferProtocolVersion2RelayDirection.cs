namespace Proxyfan.Framework.Networking;

/// <summary>
///     Identifies the direction a frame is flowing through the
///     <see cref="HypertextTransferProtocolVersion2Orchestrator" />.
/// </summary>
public enum HypertextTransferProtocolVersion2RelayDirection
{
    /// <summary>
    ///     The frame originated at the client and is being forwarded to the upstream server.
    /// </summary>
    ClientToUpstream = 0,

    /// <summary>
    ///     The frame originated at the upstream server and is being forwarded to the client.
    /// </summary>
    UpstreamToClient = 1,
}
