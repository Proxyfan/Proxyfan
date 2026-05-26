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
    private readonly IReadOnlyList<IRequestPhaseRule> _requestRules;
    private readonly IReadOnlyList<IResponsePhaseRule> _responseRules;

    /// <summary>
    ///     Initializes a new <see cref="RuleEngine" /> with the supplied request- and response-phase rules.
    ///     Rules are sorted by ascending priority once at construction time.
    /// </summary>
    /// <param name="requestRules">The collection of request-phase rules.</param>
    /// <param name="responseRules">The collection of response-phase rules.</param>
    public RuleEngine(
        IEnumerable<IRequestPhaseRule> requestRules,
        IEnumerable<IResponsePhaseRule> responseRules)
    {
        var orderedRequestRules = new List<IRequestPhaseRule>(requestRules);
        orderedRequestRules.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
        var orderedResponseRules = new List<IResponsePhaseRule>(responseRules);
        orderedResponseRules.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
        _requestRules = orderedRequestRules;
        _responseRules = orderedResponseRules;
    }

    /// <inheritdoc />
    public IReadOnlyList<RequestPipelineAction> EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var actions = new List<RequestPipelineAction>();
        var currentRequest = request;

        foreach (var rule in _requestRules)
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

        foreach (var rule in _responseRules)
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
