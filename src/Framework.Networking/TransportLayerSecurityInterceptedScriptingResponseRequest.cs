using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the response-phase scripting invocation arguments so
///     <see cref="TransportLayerSecurityInterceptedScripting.ApplyResponseAsync" /> stays under
///     the analyzer's four-parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedScriptingResponseRequest
{
    /// <summary>
    ///     Gets the request as observed by the script.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

    /// <summary>
    ///     Gets the response that the script is being given a chance to project.
    /// </summary>
    public required HypertextTransferProtocolResponseData FinalResponse { get; init; }

    /// <summary>
    ///     Gets the traffic flow whose id is supplied to the script context.
    /// </summary>
    public required TrafficFlow Flow { get; init; }

    /// <summary>
    ///     Gets the optional scripting handler to invoke.
    /// </summary>
    public IScriptingHandler? Handler { get; init; }

    /// <summary>
    ///     Gets the logger used to surface non-cancellation script exceptions.
    /// </summary>
    public required ILogger Logger { get; init; }
}
