using Proxyfan.Domain.Traffic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules.Rules;

/// <summary>
///     Defines a contract for resolving in-flight breakpoint pauses. Implementations typically
///     show a UI to the user and wait for their decision before returning.
/// </summary>
public interface IBreakpointHandler
{
    /// <summary>
    ///     Pauses the proxy pipeline and returns the user's decision for a request-phase breakpoint.
    /// </summary>
    /// <param name="request">The current request data presented to the user.</param>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>A task that completes with the user's decision.</returns>
    Task<BreakpointDecision> ResolveRequestAsync(
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Pauses the proxy pipeline and returns the user's decision for a response-phase breakpoint.
    /// </summary>
    /// <param name="request">The original request that produced this response.</param>
    /// <param name="response">The current response data presented to the user.</param>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>A task that completes with the user's decision.</returns>
    Task<BreakpointDecision> ResolveResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken);
}
