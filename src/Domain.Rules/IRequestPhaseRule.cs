using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Defines a rule that may inspect or modify an HTTP request before it leaves the proxy.
/// </summary>
public interface IRequestPhaseRule
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
    ///     Evaluates this rule against the supplied request and returns the action that the
    ///     pipeline must take, or <see langword="null" /> when the rule does not apply.
    /// </summary>
    /// <param name="request">The captured request data.</param>
    /// <returns>A <see cref="RequestPipelineAction" /> describing the rule's effect, or null.</returns>
    RequestPipelineAction? EvaluateRequest(HypertextTransferProtocolRequestData request);
}
