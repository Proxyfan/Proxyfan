using System.IO.Pipelines;
using System.Net.Sockets;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles parameters required by the SSE relay path in
///     <see cref="HypertextTransferProtocolForwarder" />. Introduced to satisfy the analyzer
///     parameter-count rule.
/// </summary>
public sealed class ServerSentEventsRelayRequest
{
    /// <summary>
    ///     Gets the originating forwarding request (carrying connection, flow, and effective request data).
    /// </summary>
    public required HypertextTransferProtocolForwardingRequest ForwardingRequest { get; init; }

    /// <summary>
    ///     Gets the parsed response header read from the upstream pipe.
    /// </summary>
    public required HypertextTransferProtocolResponseHeaderRead HeaderRead { get; init; }

    /// <summary>
    ///     Gets the pipe reader holding any buffered bytes after the response headers.
    /// </summary>
    public required PipeReader Reader { get; init; }

    /// <summary>
    ///     Gets the upstream network stream from which the SSE body will be streamed.
    /// </summary>
    public required NetworkStream UpstreamStream { get; init; }
}
