using Proxyfan.Domain.Traffic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Pipeline hook that gives the active user script the opportunity to mutate requests
///     and responses as they pass through the proxy, similar in spirit to
///     <c>IBreakpointHandler</c> but driven entirely by compiled C# code rather than
///     human-in-the-loop pauses.
/// </summary>
public interface IScriptingHandler
{
    /// <summary>
    ///     Runs the active script's request-phase logic against <paramref name="request" />.
    ///     When no script is active or the script has no request-phase hook, the request is
    ///     returned unchanged.
    /// </summary>
    /// <param name="flowId">The traffic flow identifier; used to scope flow-local shared state.</param>
    /// <param name="request">The captured request.</param>
    /// <param name="cancellationToken">A token that cancels script execution.</param>
    /// <returns>The post-script request (may equal <paramref name="request" />).</returns>
    Task<HypertextTransferProtocolRequestData> ApplyRequestAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Runs the active script's response-phase logic against <paramref name="response" />.
    ///     When no script is active or the script has no response-phase hook, the response is
    ///     returned unchanged.
    /// </summary>
    /// <param name="flowId">The traffic flow identifier; used to scope flow-local shared state.</param>
    /// <param name="request">The captured request that triggered the response.</param>
    /// <param name="response">The captured response.</param>
    /// <param name="cancellationToken">A token that cancels script execution.</param>
    /// <returns>The post-script response (may equal <paramref name="response" />).</returns>
    Task<HypertextTransferProtocolResponseData> ApplyResponseAsync(
        string flowId,
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken);
}
