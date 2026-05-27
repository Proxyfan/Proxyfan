using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parameter object that carries the connection, flow, effective request, and upstream
///     response exchange into the response-phase pipeline. Bundling these four collaborators
///     keeps the response-phase processor under the analyzer-enforced parameter cap (ATXCS022).
/// </summary>
public sealed record HypertextTransferProtocolResponsePhaseContext
{
    /// <summary>
    ///     Gets the client-side proxy connection used to write the final response.
    /// </summary>
    public required IProxyConnection Connection { get; init; }

    /// <summary>
    ///     Gets the effective request that triggered the response (after request-phase rules
    ///     and breakpoint and scripting hooks).
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the in-progress traffic flow being captured.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the upstream response exchange (response data plus raw bytes for streaming).
    /// </summary>
    public required HypertextTransferProtocolProxyResponseExchange ResponseExchange { get; init; }
}
