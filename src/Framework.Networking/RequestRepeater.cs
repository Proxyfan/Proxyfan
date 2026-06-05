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
    public async Task<Result<Guid>> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RepeatOnceAsync(originalRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            var error = BuildCancelledError(completedCount: 0, exception);
            return Result.Failure<Guid>(error);
        }
    }

    /// <inheritdoc />
    public async Task<Result<RequestReplayBatchResult>> RepeatAsync(
        HypertextTransferProtocolRequestData originalRequest,
        int repeatCount,
        TimeSpan delayBetweenRepeats,
        CancellationToken cancellationToken)
    {
        if (repeatCount < 1)
        {
            var error = BuildInvalidRepeatCountError(repeatCount);
            return Result.Failure<RequestReplayBatchResult>(error);
        }

        if (delayBetweenRepeats < TimeSpan.Zero)
        {
            var error = BuildInvalidDelayError(delayBetweenRepeats);
            return Result.Failure<RequestReplayBatchResult>(error);
        }

        var completed = 0;
        try
        {
            for (var iteration = 0; iteration < repeatCount; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (iteration > 0 && delayBetweenRepeats > TimeSpan.Zero)
                {
                    await Task.Delay(delayBetweenRepeats, _timeProvider, cancellationToken).ConfigureAwait(false);
                }

                var iterationResult = await RepeatOnceAsync(originalRequest, cancellationToken).ConfigureAwait(false);
                if (!iterationResult.IsSuccess)
                {
                    var replayError = BuildReplayErrorWithCompletedCount(iterationResult.Error!, completed);
                    return Result.Failure<RequestReplayBatchResult>(replayError);
                }

                completed++;
            }

            var batchResult = new RequestReplayBatchResult(completed, repeatCount);
            return Result.Success(batchResult);
        }
        catch (OperationCanceledException exception)
        {
            var error = BuildCancelledError(completed, exception);
            return Result.Failure<RequestReplayBatchResult>(error);
        }
    }

    private RequestReplayError BuildCancelledError(int completedCount, OperationCanceledException exception)
    {
        var message = "Request replay was cancelled.";
        var error = new RequestReplayError(RequestReplayError.CancelledCode, message, completedCount, exception);
        return error;
    }

    private RequestReplayError BuildDispatchFailedError(int completedCount, Exception exception)
    {
        var message = $"Failed to dispatch replay request: {exception.Message}";
        var error = new RequestReplayError(RequestReplayError.DispatchFailedCode, message, completedCount, exception);
        return error;
    }

    private RequestReplayError BuildInvalidDelayError(TimeSpan delayBetweenRepeats)
    {
        var message = $"Delay must not be negative. Received: {delayBetweenRepeats}.";
        var error = new RequestReplayError(RequestReplayError.InvalidDelayCode, message, completedCount: 0);
        return error;
    }

    private RequestReplayError BuildInvalidRepeatCountError(int repeatCount)
    {
        var message = $"Repeat count must be at least one. Received: {repeatCount}.";
        var error = new RequestReplayError(RequestReplayError.InvalidRepeatCountCode, message, completedCount: 0);
        return error;
    }

    private RequestReplayError BuildReplayErrorWithCompletedCount(DomainError error, int completedCount)
    {
        if (error is RequestReplayError replayError && replayError.CompletedCount == completedCount)
        {
            return replayError;
        }

        if (error is RequestReplayError existingReplayError)
        {
            if (existingReplayError.InnerException is not null)
            {
                return new RequestReplayError(
                    existingReplayError.Code,
                    existingReplayError.Message,
                    completedCount,
                    existingReplayError.InnerException);
            }

            return new RequestReplayError(
                existingReplayError.Code,
                existingReplayError.Message,
                completedCount);
        }

        if (error.InnerException is not null)
        {
            return new RequestReplayError(error.Code, error.Message, completedCount, error.InnerException);
        }

        return new RequestReplayError(error.Code, error.Message, completedCount);
    }

    private Result<Guid> BuildSingleReplayFailureResult(TrafficFlow flow, DomainError error)
    {
        var replayError = BuildReplayErrorWithCompletedCount(error, completedCount: 0);
        if (replayError.IsCancellation)
        {
            return Result.Failure<Guid>(replayError);
        }

        flow.Fail();
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);
        return Result.Failure<Guid>(replayError);
    }

    private async Task<Result<HypertextTransferProtocolResponseData>> DispatchUpstreamAsync(
        HypertextTransferProtocolRequestData effectiveRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var responseData = await _sender.SendAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);
            if (!responseData.IsSuccess)
            {
                var fallbackException = new InvalidOperationException(responseData.Error?.Message ?? "Send failed.");
                var innerException = responseData.Error?.InnerException ?? fallbackException;
                var dispatchError = BuildDispatchFailedError(completedCount: 0, innerException);
                return Result.Failure<HypertextTransferProtocolResponseData>(dispatchError);
            }

            return Result.Success(responseData.Value);
        }
        catch (OperationCanceledException exception)
        {
            var error = BuildCancelledError(completedCount: 0, exception);
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
        }
        catch (Exception exception)
        {
            var error = BuildDispatchFailedError(completedCount: 0, exception);
            return Result.Failure<HypertextTransferProtocolResponseData>(error);
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

    private async Task<Result<Guid>> RepeatOnceAsync(
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
            if (!dispatchResult.IsSuccess)
            {
                return BuildSingleReplayFailureResult(flow, dispatchResult.Error!);
            }

            responseData = dispatchResult.Value;
        }

        var responseActions = _ruleEngine.EvaluateResponse(effectiveRequest, responseData);
        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(responseData, responseActions);

        flow.SetResponse(finalResponse);
        PublishResponseReceived(flow, finalResponse);
        flow.Complete();
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);

        return Result.Success(flow.Id);
    }
}
