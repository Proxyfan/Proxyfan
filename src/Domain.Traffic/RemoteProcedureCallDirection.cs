namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Direction of a captured Remote Procedure Call (gRPC) message in a flow.
/// </summary>
public enum RemoteProcedureCallDirection
{
    /// <summary>
    ///     Client-to-server request message.
    /// </summary>
    Outbound = 0,

    /// <summary>
    ///     Server-to-client response message.
    /// </summary>
    Inbound = 1,
}
