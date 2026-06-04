using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Defines an asynchronous rule that may inspect or modify an HTTP request before it
///     leaves the proxy. Intended for rules with async side-effects such as breakpoint pauses
///     and user-script execution.
/// </summary>
public interface IAsyncRequestPhaseRule
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
    /// <param name="flowId">The traffic-flow identifier; used to scope per-flow side effects such as scripting shared state.</param>
    /// <param name="cancellationToken">A token that cancels evaluation.</param>
    /// <returns>A <see cref="RequestPipelineAction" /> describing the rule's effect, or null.</returns>
    Task<RequestPipelineAction?> EvaluateRequestAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken);
}
