using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter bundle for the forward-and-process-response step of the HTTP/1.1 proxy
///     pipeline. Required because the analyzer parameter-count rule limits methods to four
///     parameters.
/// </summary>
public sealed class HypertextTransferProtocolForwardAndProcessRequest
{
    /// <summary>
    ///     Gets the action selected by the rule engine that may serve a local response. When
    ///     <see langword="null" /> the request is forwarded to the origin server.
    /// </summary>
    public RequestPipelineAction? BlockingAction { get; init; }

    /// <summary>
    ///     Gets the client connection that receives the response.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the effective request after rules/scripting/breakpoint modifications.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for this exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the original request exchange parsed from the client connection. This is used
    ///     to build the upstream request payload while preserving the original byte sequence.
    /// </summary>
    public required HypertextTransferProtocolProxyRequestExchange RequestExchange { get; init; }
}
