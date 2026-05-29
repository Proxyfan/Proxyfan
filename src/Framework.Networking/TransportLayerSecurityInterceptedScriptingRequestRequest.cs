using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Bundles the request-phase scripting invocation arguments so
///     <see cref="TransportLayerSecurityInterceptedScripting.ApplyRequestAsync" /> stays under
///     the analyzer's four-parameter limit (ATXCS022).
/// </summary>
public sealed class TransportLayerSecurityInterceptedScriptingRequestRequest
{
    /// <summary>
    ///     Gets the request as observed by the script.
    /// </summary>
    public required HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

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
