using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object that carries the interception pipes, traffic flow, effective request,
///     and upstream response exchange into the TLS-intercepted response-phase processor.
///     Bundling these collaborators keeps the response-phase method under the analyzer-enforced
///     parameter cap (ATXCS022).
/// </summary>
public sealed record TransportLayerSecurityResponsePhaseContext
{
    /// <summary>
    ///     Gets the effective request that triggered the response (after request-phase rules,
    ///     breakpoint, and scripting hooks).
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the in-progress traffic flow being captured.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the bidirectional pipes connecting the proxy to the client and to the upstream
    ///     server through which the decrypted bytes flow.
    /// </summary>
    public required TransportLayerSecurityInterceptionPipes Pipes { get; init; }

    /// <summary>
    ///     Gets the upstream response exchange (response data plus raw bytes for streaming).
    /// </summary>
    public required HypertextTransferProtocolProxyResponseExchange ResponseExchange { get; init; }
}
