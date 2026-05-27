using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Default <see cref="IRuleEngine" /> implementation that evaluates registered request- and
///     response-phase rules in priority order, applying short-circuit semantics for blocking
///     and locally-served responses.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly IRuleRegistry _registry;

    /// <summary>
    ///     Initializes a new <see cref="RuleEngine" /> backed by the supplied registry.
    ///     Rule snapshots are queried per-evaluation so newly registered rules take effect
    ///     immediately, including across UI-driven rule edits at runtime.
    /// </summary>
    /// <param name="registry">The rule registry that supplies request- and response-phase rules.</param>
    public RuleEngine(IRuleRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    ///     Initializes a new <see cref="RuleEngine" /> seeded with the supplied rule collections.
    ///     A new <see cref="RuleRegistry" /> is created internally; subsequent registrations
    ///     are not visible through this engine.
    /// </summary>
    /// <param name="requestRules">The initial set of request-phase rules.</param>
    /// <param name="responseRules">The initial set of response-phase rules.</param>
    public RuleEngine(
        IEnumerable<IRequestPhaseRule> requestRules,
        IEnumerable<IResponsePhaseRule> responseRules)
    {
        var registry = new RuleRegistry();
        foreach (var rule in requestRules)
        {
            registry.RegisterRequestPhaseRule(rule);
        }

        foreach (var rule in responseRules)
        {
            registry.RegisterResponsePhaseRule(rule);
        }

        _registry = registry;
    }

    /// <inheritdoc />
    public IReadOnlyList<RequestPipelineAction> EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var actions = new List<RequestPipelineAction>();
        var currentRequest = request;

        foreach (var rule in _registry.GetRequestPhaseRules())
        {
            if (!rule.IsEnabled)
            {
                continue;
            }

            var action = rule.EvaluateRequest(currentRequest);

            if (action is null)
            {
                continue;
            }

            actions.Add(action);

            if (action is RequestPipelineAction.Block or RequestPipelineAction.ServeLocalResponse)
            {
                break;
            }

            if (action is RequestPipelineAction.Redirect redirectAction)
            {
                currentRequest = redirectAction.RewrittenRequest;
            }
            else if (action is RequestPipelineAction.ModifyRequest modifyAction)
            {
                currentRequest = modifyAction.ModifiedRequest;
            }
        }

        return actions;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResponsePipelineAction> EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        var actions = new List<ResponsePipelineAction>();
        var currentResponse = response;

        foreach (var rule in _registry.GetResponsePhaseRules())
        {
            if (!rule.IsEnabled)
            {
                continue;
            }

            var action = rule.EvaluateResponse(request, currentResponse);

            if (action is null)
            {
                continue;
            }

            actions.Add(action);

            if (action is ResponsePipelineAction.ModifyResponse modifyAction)
            {
                currentResponse = modifyAction.ModifiedResponse;
            }
        }

        return actions;
    }
}
