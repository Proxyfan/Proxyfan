using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Evaluates registered rules against requests and responses, yielding the actions the
///     proxy pipeline must take.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    ///     Evaluates the request-phase rules in order. The first short-circuiting rule
    ///     (e.g., Block, ServeLocalResponse) ends evaluation and is returned alone.
    ///     Modifying rules (Redirect, ModifyRequest) update the request in-place for the next rule.
    /// </summary>
    /// <param name="request">The captured request data.</param>
    /// <returns>The ordered set of actions to apply to the request.</returns>
    IReadOnlyList<RequestPipelineAction> EvaluateRequest(HypertextTransferProtocolRequestData request);

    /// <summary>
    ///     Evaluates the response-phase rules in order. Modifying rules update the response for the next rule.
    /// </summary>
    /// <param name="request">The captured request that produced the response.</param>
    /// <param name="response">The captured response data.</param>
    /// <returns>The ordered set of actions to apply to the response.</returns>
    IReadOnlyList<ResponsePipelineAction> EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response);
}
