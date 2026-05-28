using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object that bundles the inputs needed by the HTTP/1.1 proxy handler's
///     upstream forwarding step. Required to stay within the analyzer's 4-parameter
///     constructor/method limit (ATXCS022). The forwarding step may either return a fully-read
///     response exchange or stream a long-lived response (e.g. Server-Sent Events) directly to
///     the client, which is why it needs access to the client connection, traffic flow, and
///     effective request in addition to the original request exchange.
/// </summary>
public sealed class HypertextTransferProtocolForwardingRequest
{
    /// <summary>
    ///     Gets the client-facing proxy connection. Used to write the response when the upstream
    ///     body is streamed directly to the client (e.g. SSE).
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the request as modified by rules, scripting, and breakpoints. Determines the
    ///     keep-alive policy and is passed to the streaming relay for callbacks.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for this exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the request exchange (header bytes + body) to forward upstream after any
    ///     hop-by-hop header rewriting.
    /// </summary>
    public required HypertextTransferProtocolProxyRequestExchange RequestExchange { get; init; }
}
