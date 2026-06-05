using Proxyfan.Domain;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pipeline-aware <see cref="IRequestRepeater" /> implementation that runs the same rule
///     engine the proxy listener uses, dispatches the (possibly modified) request through
///     <see cref="IComposerRequestSender" />, records the result as a fresh
///     <see cref="TrafficFlow" /> with <see cref="TrafficFlowOrigin.Repeated" />, and publishes
///     the standard <see cref="RequestReceived" />, <see cref="ResponseReceived" /> and
///     <see cref="TrafficFlowCompleted" /> domain events so the UI surfaces it like any other
///     captured exchange.
/// </summary>
public sealed class RequestRepeater : IRequestRepeater
{
    private const string ClientEndPointLabel = "(repeat)";
    private readonly IDomainEventBus _eventBus;
    private readonly IRuleEngine _ruleEngine;
    private readonly IComposerRequestSender _sender;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;

    /// <summary>
    ///     Initializes a new <see cref="RequestRepeater" />.
    /// </summary>
    /// <param name="sender">The sender used to dispatch the (possibly modified) request upstream.</param>
    /// <param name="ruleEngine">The rule engine to evaluate against the request and response.</param>
    /// <param name="trafficStore">The store the resulting flow is added to.</param>
    /// <param name="eventBus">The event bus the lifecycle events are published on.</param>
    /// <param name="timeProvider">Time source used for timestamps.</param>
    public RequestRepeater(
        IComposerRequestSender sender,
        IRuleEngine ruleEngine,
        ITrafficStore trafficStore,
        IDomainEventBus eventBus,
        TimeProvider timeProvider)
    {
        _eventBus = eventBus;
        _ruleEngine = ruleEngine;
        _sender = sender;
        _timeProvider = timeProvider;
        _trafficStore = trafficStore;
    }

    /// <inheritdoc />
    public async Task<Guid> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken)
    {
        var flowId = await RepeatOnceAsync(originalRequest, cancellationToken).ConfigureAwait(false);
        return flowId;
    }

    /// <inheritdoc />
    public async Task<int> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        int repeatCount,
        TimeSpan delayBetweenRepeats,
        CancellationToken cancellationToken)
    {
        if (repeatCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatCount), repeatCount, "Repeat count must be at least one.");
        }

        if (delayBetweenRepeats < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delayBetweenRepeats), delayBetweenRepeats, "Delay must not be negative.");
        }

        var completed = 0;
        for (var iteration = 0; iteration < repeatCount; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (iteration > 0 && delayBetweenRepeats > TimeSpan.Zero)
            {
                await Task.Delay(delayBetweenRepeats, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            await RepeatOnceAsync(originalRequest, cancellationToken).ConfigureAwait(false);
            completed++;
        }

        return completed;
    }

    private async Task<HypertextTransferProtocolResponseData?> DispatchUpstreamAsync(
        HypertextTransferProtocolRequestData effectiveRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var responseData = await _sender.SendAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);
            if (!responseData.IsSuccess)
            {
                return null;
            }

            return responseData.Value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void PublishFlowCompleted(TrafficFlow flow)
    {
        var completed = new TrafficFlowCompleted(flow.Id, flow.Status, _timeProvider.GetUtcNow());
        _eventBus.Publish(completed);
    }

    private void PublishRequestReceived(TrafficFlow flow, HypertextTransferProtocolRequestData request)
    {
        var requestReceived = new RequestReceived(flow.Id, request, flow.ClientEndPoint, _timeProvider.GetUtcNow());
        _eventBus.Publish(requestReceived);
    }

    private void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceived = new ResponseReceived(flow.Id, response, _timeProvider.GetUtcNow());
        _eventBus.Publish(responseReceived);
    }

    private async Task<Guid> RepeatOnceAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetUtcNow();
        var flowId = Guid.NewGuid();
        var flow = new TrafficFlow(flowId, ClientEndPointLabel, startedAt, TrafficFlowOrigin.Repeated);

        var requestActions = _ruleEngine.EvaluateRequest(originalRequest);
        var effectiveRequest = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(originalRequest, requestActions);

        flow.SetRequest(effectiveRequest);
        PublishRequestReceived(flow, effectiveRequest);

        var blockingAction = HypertextTransferProtocolRuleApplicator.FindBlockingAction(requestActions);

        HypertextTransferProtocolResponseData responseData;
        if (blockingAction is RequestPipelineAction.Block)
        {
            responseData = HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData();
        }
        else if (blockingAction is RequestPipelineAction.ServeLocalResponse serveAction)
        {
            responseData = serveAction.LocalResponse;
        }
        else
        {
            var dispatchResult = await DispatchUpstreamAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);
            if (dispatchResult is null)
            {
                flow.Fail();
                _trafficStore.Add(flow);
                PublishFlowCompleted(flow);
                return flow.Id;
            }

            responseData = dispatchResult;
        }

        var responseActions = _ruleEngine.EvaluateResponse(effectiveRequest, responseData);
        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(responseData, responseActions);

        flow.SetResponse(finalResponse);
        PublishResponseReceived(flow, finalResponse);
        flow.Complete();
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);

        return flow.Id;
    }
}
