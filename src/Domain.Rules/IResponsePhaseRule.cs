using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Defines a rule that may inspect or modify an HTTP response before it is delivered to the client.
/// </summary>
public interface IResponsePhaseRule
{
    /// <summary>
    ///     Gets a value indicating whether this rule is currently enabled.
    ///     Disabled rules are skipped by the rule engine.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Gets the rule's priority within its rule type. Lower values are evaluated first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Evaluates this rule against the supplied request/response pair and returns the action
    ///     that the pipeline must take, or <see langword="null" /> when the rule does not apply.
    /// </summary>
    /// <param name="request">The captured request that produced the response.</param>
    /// <param name="response">The captured response data.</param>
    /// <returns>A <see cref="ResponsePipelineAction" /> describing the rule's effect, or null.</returns>
    ResponsePipelineAction? EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response);
}
