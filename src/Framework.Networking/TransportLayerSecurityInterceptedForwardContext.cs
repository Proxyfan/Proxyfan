using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object bundling the state required by
///     <see cref="TransportLayerSecurityInterceptorHandler" /> when forwarding a
///     non-upgrade intercepted request to the upstream server. Required to keep the
///     forwarding method below the analyzer's four-parameter limit (ATXCS022).
/// </summary>
public sealed record TransportLayerSecurityInterceptedForwardContext
{
    /// <summary>
    ///     Gets the effective request after rules, breakpoints, and scripting modifications.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the traffic flow that accumulates capture data for this exchange.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the four pipes used by the TLS-intercepted HTTP loop.
    /// </summary>
    public required TransportLayerSecurityInterceptionPipes Pipes { get; init; }

    /// <summary>
    ///     Gets the original request exchange (headers + body) as received from the client.
    /// </summary>
    public required HypertextTransferProtocolProxyRequestExchange RequestExchange { get; init; }

    /// <summary>
    ///     Gets the optional <see cref="RequestPipelineAction.ServeLocalResponse" /> emitted
    ///     by Map-Local rules when the upstream call should be skipped.
    /// </summary>
    public RequestPipelineAction.ServeLocalResponse? ServeLocal { get; init; }
}
