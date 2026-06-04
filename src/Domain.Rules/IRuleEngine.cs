using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Evaluates registered rules against requests and responses, yielding the actions the
///     proxy pipeline must take.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    ///     Evaluates the synchronous request-phase rules in order. The first short-circuiting rule
    ///     (e.g., Block, ServeLocalResponse) ends evaluation and is returned alone.
    ///     Modifying rules (Redirect, ModifyRequest) update the request in-place for the next rule.
    ///     Async rules registered with the engine are <b>not</b> evaluated by this method; use
    ///     <see cref="EvaluateRequestAsync" /> when async rules must participate.
    /// </summary>
    /// <param name="request">The captured request data.</param>
    /// <returns>The ordered set of actions to apply to the request.</returns>
    IReadOnlyList<RequestPipelineAction> EvaluateRequest(HypertextTransferProtocolRequestData request);

    /// <summary>
    ///     Evaluates all request-phase rules — both synchronous and asynchronous — in priority
    ///     order. Short-circuiting actions (<see cref="RequestPipelineAction.Block" />,
    ///     <see cref="RequestPipelineAction.ServeLocalResponse" />,
    ///     <see cref="RequestPipelineAction.Pause" />) end evaluation immediately. Modifying
    ///     rules (<see cref="RequestPipelineAction.Redirect" />,
    ///     <see cref="RequestPipelineAction.ModifyRequest" />) update the working request for
    ///     subsequent rules.
    /// </summary>
    /// <param name="request">The captured request data.</param>
    /// <param name="flowId">The traffic-flow identifier; passed to async rules that require per-flow state.</param>
    /// <param name="cancellationToken">A token that cancels evaluation.</param>
    /// <returns>The ordered set of actions to apply to the request.</returns>
    Task<IReadOnlyList<RequestPipelineAction>> EvaluateRequestAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Evaluates the synchronous response-phase rules in order. Modifying rules update the
    ///     response for the next rule. Async rules registered with the engine are <b>not</b>
    ///     evaluated by this method; use <see cref="EvaluateResponseAsync" /> when async rules
    ///     must participate.
    /// </summary>
    /// <param name="request">The captured request that produced the response.</param>
    /// <param name="response">The captured response data.</param>
    /// <returns>The ordered set of actions to apply to the response.</returns>
    IReadOnlyList<ResponsePipelineAction> EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response);

    /// <summary>
    ///     Evaluates all response-phase rules — both synchronous and asynchronous — in priority
    ///     order. Modifying rules update the working response for subsequent rules. A
    ///     <see cref="ResponsePipelineAction.Pause" /> action short-circuits evaluation.
    /// </summary>
    /// <param name="request">The captured request that produced the response.</param>
    /// <param name="response">The captured response data.</param>
    /// <param name="flowId">The traffic-flow identifier; passed to async rules that require per-flow state.</param>
    /// <param name="cancellationToken">A token that cancels evaluation.</param>
    /// <returns>The ordered set of actions to apply to the response.</returns>
    Task<IReadOnlyList<ResponsePipelineAction>> EvaluateResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        string flowId,
        CancellationToken cancellationToken);
}
