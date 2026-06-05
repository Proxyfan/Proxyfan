using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Rules;

/// <summary>
///     Default <see cref="IRuleEngine" /> implementation that evaluates registered request- and
///     response-phase rules in priority order, applying short-circuit semantics for blocking
///     and locally-served responses.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly IRuleRegistry _registry;

    /// <summary>
    ///     Initializes a new <see cref="RuleEngine" /> backed by the supplied registry.
    ///     Rule snapshots are queried per-evaluation so newly registered rules take effect
    ///     immediately, including across UI-driven rule edits at runtime.
    /// </summary>
    /// <param name="registry">The rule registry that supplies request- and response-phase rules.</param>
    /// <param name="logger">The logger used to surface non-fatal rule evaluation errors.</param>
    public RuleEngine(IRuleRegistry registry, ILogger<RuleEngine> logger)
    {
        _registry = registry;
        _logger = logger;
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
        _logger = NullLogger<RuleEngine>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<RequestPipelineAction> EvaluateRequest(HypertextTransferProtocolRequestData request)
    {
        var result = RunSyncRequestPhase(request);
        return result.Actions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RequestPipelineAction>> EvaluateRequestAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken)
    {
        var syncResult = RunSyncRequestPhase(request);

        if (syncResult.IsShortCircuited)
        {
            return syncResult.Actions;
        }

        var asyncActions = await RunAsyncRequestPhaseAsync(syncResult.EffectiveRequest, flowId, cancellationToken).ConfigureAwait(false);

        if (asyncActions.Count == 0)
        {
            return syncResult.Actions;
        }

        var combined = new List<RequestPipelineAction>(syncResult.Actions);
        combined.AddRange(asyncActions);
        return combined;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResponsePipelineAction> EvaluateResponse(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        var result = RunSyncResponsePhase(request, response);
        return result.Actions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResponsePipelineAction>> EvaluateResponseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        string flowId,
        CancellationToken cancellationToken)
    {
        var syncResult = RunSyncResponsePhase(request, response);
        var asyncActions = await RunAsyncResponsePhaseAsync(request, syncResult.EffectiveResponse, flowId, cancellationToken).ConfigureAwait(false);

        if (asyncActions.Count == 0)
        {
            return syncResult.Actions;
        }

        var combined = new List<ResponsePipelineAction>(syncResult.Actions);
        combined.AddRange(asyncActions);
        return combined;
    }

    private async Task<List<RequestPipelineAction>> RunAsyncRequestPhaseAsync(
        HypertextTransferProtocolRequestData request,
        string flowId,
        CancellationToken cancellationToken)
    {
        var actions = new List<RequestPipelineAction>();
        var currentRequest = request;

        foreach (var rule in _registry.GetAsyncRequestPhaseRules())
        {
            if (!rule.IsEnabled)
            {
                continue;
            }

            RequestPipelineAction? action;
            try
            {
                action = await rule.EvaluateRequestAsync(currentRequest, flowId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Async request-phase rule {RuleType} threw; skipping rule", rule.GetType().Name);
                continue;
            }

            if (action is null)
            {
                continue;
            }

            actions.Add(action);

            if (action is RequestPipelineAction.Block or RequestPipelineAction.Pause or RequestPipelineAction.ServeLocalResponse)
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

    private async Task<List<ResponsePipelineAction>> RunAsyncResponsePhaseAsync(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response,
        string flowId,
        CancellationToken cancellationToken)
    {
        var actions = new List<ResponsePipelineAction>();
        var currentResponse = response;

        foreach (var rule in _registry.GetAsyncResponsePhaseRules())
        {
            if (!rule.IsEnabled)
            {
                continue;
            }

            ResponsePipelineAction? action;
            try
            {
                action = await rule.EvaluateResponseAsync(request, currentResponse, flowId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Async response-phase rule {RuleType} threw; skipping rule", rule.GetType().Name);
                continue;
            }

            if (action is null)
            {
                continue;
            }

            actions.Add(action);

            if (action is ResponsePipelineAction.Pause)
            {
                break;
            }

            if (action is ResponsePipelineAction.ModifyResponse modifyAction)
            {
                currentResponse = modifyAction.ModifiedResponse;
            }
        }

        return actions;
    }

    private SyncRequestPhaseResult RunSyncRequestPhase(HypertextTransferProtocolRequestData request)
    {
        var actions = new List<RequestPipelineAction>();
        var currentRequest = request;

        foreach (var rule in _registry.GetRequestPhaseRules())
        {
            if (!rule.IsEnabled)
            {
                continue;
            }

            RequestPipelineAction? action;
            try
            {
                action = rule.EvaluateRequest(currentRequest);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Sync request-phase rule {RuleType} threw; skipping rule", rule.GetType().Name);
                continue;
            }

            if (action is null)
            {
                continue;
            }

            actions.Add(action);

            if (action is RequestPipelineAction.Block or RequestPipelineAction.ServeLocalResponse)
            {
                return new SyncRequestPhaseResult(actions, currentRequest, true);
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

        return new SyncRequestPhaseResult(actions, currentRequest, false);
    }

    private SyncResponsePhaseResult RunSyncResponsePhase(
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

            ResponsePipelineAction? action;
            try
            {
                action = rule.EvaluateResponse(request, currentResponse);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Sync response-phase rule {RuleType} threw; skipping rule", rule.GetType().Name);
                continue;
            }

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

        return new SyncResponsePhaseResult(actions, currentResponse);
    }

    private sealed record SyncRequestPhaseResult
    {
        public List<RequestPipelineAction> Actions { get; init; }

        public HypertextTransferProtocolRequestData EffectiveRequest { get; init; }

        public bool IsShortCircuited { get; init; }

        public SyncRequestPhaseResult(
            List<RequestPipelineAction> actions,
            HypertextTransferProtocolRequestData effectiveRequest,
            bool shortCircuited)
        {
            Actions = actions;
            EffectiveRequest = effectiveRequest;
            IsShortCircuited = shortCircuited;
        }
    }

    private sealed record SyncResponsePhaseResult
    {
        public List<ResponsePipelineAction> Actions { get; init; }

        public HypertextTransferProtocolResponseData EffectiveResponse { get; init; }

        public SyncResponsePhaseResult(
            List<ResponsePipelineAction> actions,
            HypertextTransferProtocolResponseData effectiveResponse)
        {
            Actions = actions;
            EffectiveResponse = effectiveResponse;
        }
    }
}
