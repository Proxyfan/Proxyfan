using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Handles plain HTTP/1.1 proxy requests by forwarding them to the upstream origin,
///     capturing request and response data, and storing completed traffic flows.
/// </summary>
public sealed class HypertextTransferProtocolProxyHandler : IConnectionHandler
{
    private const int DefaultHypertextTransferProtocolPort = 80;
    private const int MaxHeaderBytes = 65536;
    private static readonly byte[][] MethodPrefixes;
    private readonly IBreakpointHandler? _breakpointHandler;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger<HypertextTransferProtocolProxyHandler> _logger;
    private readonly IRuleEngine _ruleEngine;
    private readonly IScriptingHandler? _scriptingHandler;
    private readonly MutableThrottleProfile? _throttleProfile;
    private readonly ITrafficStore _trafficStore;
    private readonly IOptionsMonitor<UpstreamProxyOptions>? _upstreamProxy;

    static HypertextTransferProtocolProxyHandler()
    {
        var methodPrefixes = new byte[][]
        {
            Encoding.ASCII.GetBytes("DELETE "),
            Encoding.ASCII.GetBytes("GET "),
            Encoding.ASCII.GetBytes("HEAD "),
            Encoding.ASCII.GetBytes("OPTIONS "),
            Encoding.ASCII.GetBytes("PATCH "),
            Encoding.ASCII.GetBytes("POST "),
            Encoding.ASCII.GetBytes("PUT "),
            Encoding.ASCII.GetBytes("TRACE "),
        };
        MethodPrefixes = methodPrefixes;
    }

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolProxyHandler" /> instance.
    /// </summary>
    /// <param name="dependencies">The bundled handler dependencies.</param>
    public HypertextTransferProtocolProxyHandler(HypertextTransferProtocolProxyHandlerDependencies dependencies)
    {
        _trafficStore = dependencies.TrafficStore;
        _eventBus = dependencies.EventBus;
        _ruleEngine = dependencies.RuleEngine;
        _scriptingHandler = dependencies.ScriptingHandler;
        _logger = dependencies.Logger;
        _upstreamProxy = dependencies.UpstreamProxy;
        _throttleProfile = dependencies.ThrottleProfile;
        _breakpointHandler = dependencies.BreakpointHandler;
    }

    /// <inheritdoc />
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        foreach (var methodPrefix in MethodPrefixes)
        {
            if (CanStartWith(initialBytes, methodPrefix))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers
                .ReadRequestAsync(connection.Transport.Input, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (requestExchange is null)
            {
                return;
            }

            var canContinue = await ProcessSingleExchangeAsync(connection, requestExchange, cancellationToken).ConfigureAwait(false);

            if (!canContinue)
            {
                return;
            }
        }
    }

    private async Task<BreakpointDecision> ApplyRequestBreakpointAsync(
        HypertextTransferProtocolRequestData effectiveRequest,
        RequestPipelineAction? blockingAction,
        CancellationToken cancellationToken)
    {
        if (_breakpointHandler is null || blockingAction is RequestPipelineAction.ServeLocalResponse)
        {
            return BreakpointDecisions.ResumeRequest(effectiveRequest);
        }

        var decision = await _breakpointHandler.ResolveRequestAsync(effectiveRequest, cancellationToken).ConfigureAwait(false);
        return decision;
    }

    private async Task<BreakpointDecision> ApplyResponseBreakpointAsync(
        HypertextTransferProtocolRequestData effectiveRequest,
        HypertextTransferProtocolResponseData finalResponse,
        CancellationToken cancellationToken)
    {
        if (_breakpointHandler is null)
        {
            return BreakpointDecisions.ResumeResponse(finalResponse);
        }

        var decision = await _breakpointHandler.ResolveResponseAsync(effectiveRequest, finalResponse, cancellationToken).ConfigureAwait(false);
        return decision;
    }

    private async Task<HypertextTransferProtocolRequestData> ApplyScriptingRequestAsync(
        TrafficFlow flow,
        HypertextTransferProtocolRequestData effectiveRequest,
        CancellationToken cancellationToken)
    {
        if (_scriptingHandler is null)
        {
            return effectiveRequest;
        }

        try
        {
            var flowId = flow.Id.ToString();
            var projected = await _scriptingHandler.ApplyRequestAsync(flowId, effectiveRequest, cancellationToken).ConfigureAwait(false);
            return projected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scripting request-phase hook threw; continuing with unmodified request");
            return effectiveRequest;
        }
    }

    private async Task<HypertextTransferProtocolResponseData> ApplyScriptingResponseAsync(
        TrafficFlow flow,
        HypertextTransferProtocolRequestData effectiveRequest,
        HypertextTransferProtocolResponseData finalResponse,
        CancellationToken cancellationToken)
    {
        if (_scriptingHandler is null)
        {
            return finalResponse;
        }

        try
        {
            var flowId = flow.Id.ToString();
            var projected = await _scriptingHandler.ApplyResponseAsync(flowId, effectiveRequest, finalResponse, cancellationToken).ConfigureAwait(false);
            return projected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scripting response-phase hook threw; continuing with unmodified response");
            return finalResponse;
        }
    }

    private async Task ApplyThrottleAsync(CancellationToken cancellationToken)
    {
        await ThrottleApplier.ApplyLatencyAsync(_throttleProfile, cancellationToken).ConfigureAwait(false);
    }

    private bool CanKeepClientConnectionAlive(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        if (string.Equals(request.Version, "HTTP/1.0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!response.Headers.HasHeader("Content-Length"))
        {
            return false;
        }

        if (HasConnectionCloseDirective(request.Headers) || HasConnectionCloseDirective(response.Headers))
        {
            return false;
        }

        return true;
    }

    private bool CanStartWith(ReadOnlySequence<byte> initialBytes, byte[] prefix)
    {
        if (initialBytes.Length < prefix.Length)
        {
            return false;
        }

        Span<byte> candidatePrefix = stackalloc byte[prefix.Length];
        initialBytes.Slice(0, prefix.Length).CopyTo(candidatePrefix);
        return candidatePrefix.SequenceEqual(prefix);
    }

    private TrafficFlow CreateTrafficFlow(IProxyConnection connection)
    {
        var clientEndPoint = connection.RemoteEndPoint?.ToString() ?? "unknown";
        var flow = new TrafficFlow(Guid.NewGuid(), clientEndPoint, DateTimeOffset.UtcNow);
        return flow;
    }

    private void FailAndCompleteFlow(TrafficFlow flow)
    {
        flow.Fail();
        PublishFlowCompleted(flow);
    }

    private async Task<HypertextTransferProtocolProxyResponseExchange?> ForwardRequestAsync(
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var hostEndpoint = ParseHostEndpoint(requestExchange.Request.Headers);

        if (hostEndpoint is null)
        {
            _logger.LogDebug("HTTP request is missing a valid Host header.");
            return null;
        }

        var upstreamOptions = _upstreamProxy?.CurrentValue;
        var hasUpstreamProxy = upstreamOptions is not null
            && upstreamOptions.HasValidConfiguration()
            && !BypassPatternMatcher.HasMatch(upstreamOptions.BypassPatterns, hostEndpoint.Host);
        ConnectTarget? upstreamTarget = null;
        if (hasUpstreamProxy)
        {
            var built = new ConnectTarget(upstreamOptions!.Host!, upstreamOptions.Port);
            upstreamTarget = built;
        }

        var connectTarget = upstreamTarget ?? hostEndpoint;
        var proxyAuthorization = hasUpstreamProxy ? ProxyAuthorizationHeader.Build(upstreamOptions!) : null;
        var headerBytes = hasUpstreamProxy
            ? UpstreamProxyRequestRewriter.RewriteHeaders(requestExchange.HeaderBytes, requestExchange.Request, proxyAuthorization)
            : requestExchange.HeaderBytes;
        using var upstreamClient = new TcpClient();
        await upstreamClient.ConnectAsync(connectTarget.Host, connectTarget.Port, cancellationToken).ConfigureAwait(false);
        await using var upstreamStream = upstreamClient.GetStream();
        await upstreamStream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await upstreamStream.WriteAsync(requestExchange.Body, cancellationToken).ConfigureAwait(false);
        await upstreamStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        var reader = PipeReader.Create(upstreamStream);
        var responseExchange = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(reader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);
        await reader.CompleteAsync().ConfigureAwait(false);
        return responseExchange;
    }

    private async Task<HypertextTransferProtocolProxyResponseExchange?> GetResponseExchangeAsync(
        RequestPipelineAction? blockingAction,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        HypertextTransferProtocolRequestData effectiveRequest,
        CancellationToken cancellationToken)
    {
        if (blockingAction is RequestPipelineAction.ServeLocalResponse serveAction)
        {
            var localExchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(serveAction.LocalResponse);
            return localExchange;
        }

        var requestForUpstream = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(requestExchange, effectiveRequest);
        var exchange = await ForwardRequestAsync(requestForUpstream, cancellationToken).ConfigureAwait(false);
        return exchange;
    }

    private async Task HandleBlockedRequestAsync(IProxyConnection connection, TrafficFlow flow, CancellationToken cancellationToken)
    {
        await HypertextTransferProtocolRuleApplicator.SendBlockedResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
        flow.SetResponse(HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData());
        flow.Complete();
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);
    }

    private bool HasConnectionCloseDirective(HeaderCollection headers)
    {
        var connectionValue = headers.Get("Connection");

        if (string.IsNullOrWhiteSpace(connectionValue))
        {
            return false;
        }

        return connectionValue.Contains("close", StringComparison.OrdinalIgnoreCase);
    }

    private ConnectTarget? ParseHostEndpoint(HeaderCollection headers)
    {
        var hostValue = headers.Get("Host");

        if (string.IsNullOrWhiteSpace(hostValue))
        {
            return null;
        }

        var separatorIndex = hostValue.LastIndexOf(':');

        if (separatorIndex < 0)
        {
            var hostWithoutPort = hostValue.Trim();
            var defaultTarget = new ConnectTarget(hostWithoutPort, DefaultHypertextTransferProtocolPort);
            return defaultTarget;
        }

        var host = hostValue[..separatorIndex].Trim();
        var portText = hostValue[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(host) || !int.TryParse(portText, out var port) || port is < 1 or > 65535)
        {
            return null;
        }

        var target = new ConnectTarget(host, port);
        return target;
    }

    private async Task<bool> ProcessResponsePhaseAsync(
        HypertextTransferProtocolResponsePhaseContext context,
        CancellationToken cancellationToken)
    {
        var flow = context.Flow;
        var effectiveRequest = context.EffectiveRequest;
        var responseExchange = context.ResponseExchange;
        var responseActions = _ruleEngine.EvaluateResponse(effectiveRequest, responseExchange.Response);
        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(responseExchange.Response, responseActions);

        finalResponse = await ApplyScriptingResponseAsync(flow, effectiveRequest, finalResponse, cancellationToken).ConfigureAwait(false);

        var responseBreakpoint = await ApplyResponseBreakpointAsync(effectiveRequest, finalResponse, cancellationToken).ConfigureAwait(false);
        if (responseBreakpoint.IsAborting)
        {
            FailAndCompleteFlow(flow);
            return false;
        }
        finalResponse = responseBreakpoint.ModifiedResponse ?? finalResponse;

        var finalExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(responseExchange, finalResponse);
        flow.SetResponse(finalResponse);
        PublishResponseReceived(flow, finalResponse);
        flow.Complete();
        await ApplyThrottleAsync(cancellationToken).ConfigureAwait(false);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(context.Connection.Transport.Output, finalExchange, cancellationToken).ConfigureAwait(false);
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);
        return CanKeepClientConnectionAlive(effectiveRequest, finalResponse);
    }

    private async Task<bool> ProcessSingleExchangeAsync(
        IProxyConnection connection,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var flow = CreateTrafficFlow(connection);
        PublishFlowCreated(flow);
        flow.SetRequest(requestExchange.Request);
        PublishRequestReceived(flow, requestExchange.Request);

        var requestActions = _ruleEngine.EvaluateRequest(requestExchange.Request);
        var effectiveRequest = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(requestExchange.Request, requestActions);
        var blockingAction = HypertextTransferProtocolRuleApplicator.FindBlockingAction(requestActions);

        if (blockingAction is RequestPipelineAction.Block)
        {
            await HandleBlockedRequestAsync(connection, flow, cancellationToken).ConfigureAwait(false);
            return false;
        }

        var requestBreakpoint = await ApplyRequestBreakpointAsync(effectiveRequest, blockingAction, cancellationToken).ConfigureAwait(false);
        if (requestBreakpoint.IsAborting)
        {
            FailAndCompleteFlow(flow);
            return false;
        }
        effectiveRequest = requestBreakpoint.ModifiedRequest ?? effectiveRequest;

        effectiveRequest = await ApplyScriptingRequestAsync(flow, effectiveRequest, cancellationToken).ConfigureAwait(false);

        var responseExchange = await GetResponseExchangeAsync(blockingAction, requestExchange, effectiveRequest, cancellationToken).ConfigureAwait(false);
        if (responseExchange is null)
        {
            FailAndCompleteFlow(flow);
            return false;
        }

        var context = new HypertextTransferProtocolResponsePhaseContext
        {
            Connection = connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            ResponseExchange = responseExchange,
        };
        return await ProcessResponsePhaseAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private void PublishFlowCompleted(TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
    }

    private void PublishFlowCreated(TrafficFlow flow)
    {
        var createdEvent = new TrafficFlowCreated(flow.Id, DateTimeOffset.UtcNow);
        _eventBus.Publish(createdEvent);
    }

    private void PublishRequestReceived(TrafficFlow flow, HypertextTransferProtocolRequestData request)
    {
        var requestReceivedEvent = new RequestReceived(flow.Id, request, flow.ClientEndPoint, DateTimeOffset.UtcNow);
        _eventBus.Publish(requestReceivedEvent);
    }

    private void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        _eventBus.Publish(responseReceivedEvent);
    }
}
