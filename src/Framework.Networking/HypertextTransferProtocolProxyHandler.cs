using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Certificates;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Rules.Rules;
using Proxyfan.Domain.Scripting;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
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
    private readonly MutableCertificateAuthorityProvider? _certificateAuthorityProvider;
    private readonly HypertextTransferProtocolFlowEventPublisher _flowEventPublisher;
    private readonly HypertextTransferProtocolForwarder _forwarder;
    private readonly ILogger<HypertextTransferProtocolProxyHandler> _logger;
    private readonly PacketLossSampler _packetLossSampler;
    private readonly IRuleEngine _ruleEngine;
    private readonly IScriptingHandler? _scriptingHandler;
    private readonly MutableThrottleProfile? _throttleProfile;
    private readonly ITrafficStore _trafficStore;
    private readonly HypertextTransferProtocolUpgradeOrchestrator _upgradeOrchestrator;

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
        _ruleEngine = dependencies.RuleEngine;
        _scriptingHandler = dependencies.ScriptingHandler;
        _logger = dependencies.Logger;
        _throttleProfile = dependencies.ThrottleProfile;
        _breakpointHandler = dependencies.BreakpointHandler;
        _certificateAuthorityProvider = dependencies.CertificateAuthorityProvider;
        _packetLossSampler = dependencies.PacketLossSampler ?? DefaultPacketLossSamplers.Shared;
        var timeProvider = dependencies.TimeProvider ?? TimeProvider.System;
        var flowEventPublisher = new HypertextTransferProtocolFlowEventPublisher(dependencies.EventBus);
        _flowEventPublisher = flowEventPublisher;
        var forwarderDependencies = new HypertextTransferProtocolForwarderDependencies
        {
            EventBus = dependencies.EventBus,
            HostResolver = dependencies.HostResolver,
            Logger = dependencies.Logger,
            ServerSentEventsStore = dependencies.ServerSentEventsStore,
            ThrottleProfile = dependencies.ThrottleProfile,
            TimeProvider = timeProvider,
            TrafficStore = dependencies.TrafficStore,
            UpstreamProxy = dependencies.UpstreamProxy,
        };
        var forwarder = new HypertextTransferProtocolForwarder(forwarderDependencies);
        _forwarder = forwarder;
        var upgradeOrchestratorDependencies = new HypertextTransferProtocolUpgradeOrchestratorDependencies
        {
            FlowEventPublisher = _flowEventPublisher,
            HostResolver = dependencies.HostResolver,
            TimeProvider = timeProvider,
            TrafficStore = dependencies.TrafficStore,
            WebSocketStore = dependencies.WebSocketStore,
        };
        var upgradeOrchestrator = new HypertextTransferProtocolUpgradeOrchestrator(upgradeOrchestratorDependencies);
        _upgradeOrchestrator = upgradeOrchestrator;
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
        RequestPipelineAction? blockingAction,
        CancellationToken cancellationToken)
    {
        if (_scriptingHandler is null || blockingAction is RequestPipelineAction.ServeLocalResponse)
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

    private async Task<bool> DispatchUpgradeExchangeAsync(
        UpgradeExchangeRequest request,
        CancellationToken cancellationToken)
    {
        var hostEndpoint = ParseHostEndpoint(request.EffectiveRequest.Headers);

        if (hostEndpoint is null)
        {
            _logger.LogDebug("WebSocket upgrade request is missing a valid Host header.");
            FailAndCompleteFlow(request.Flow);
            return false;
        }

        var dispatched = await _upgradeOrchestrator.DispatchAsync(request, hostEndpoint, cancellationToken).ConfigureAwait(false);
        return dispatched;
    }

    private void FailAndCompleteFlow(TrafficFlow flow)
    {
        flow.Fail();
        _flowEventPublisher.PublishFlowCompleted(flow);
    }

    private async Task<bool> ForwardAndProcessResponseAsync(
        HypertextTransferProtocolForwardAndProcessRequest request,
        CancellationToken cancellationToken)
    {
        var flow = request.Flow;
        var effectiveRequest = request.EffectiveRequest;
        var requestForUpstream = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(request.RequestExchange, effectiveRequest);
        var forwardingRequest = new HypertextTransferProtocolForwardingRequest
        {
            Connection = request.Connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            RequestExchange = requestForUpstream,
        };
        var outcome = await GetResponseExchangeAsync(forwardingRequest, request.BlockingAction, cancellationToken).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            FailAndCompleteFlow(flow);
            return false;
        }

        if (outcome.IsStreaming)
        {
            return false;
        }

        var context = HypertextTransferProtocolResponsePhaseContextFactory.Create(request.Connection, effectiveRequest, flow, outcome.Exchange!);
        return await ProcessResponsePhaseAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HypertextTransferProtocolForwardingOutcome> ForwardRequestAsync(
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        CancellationToken cancellationToken)
    {
        var requestExchange = forwardingRequest.RequestExchange;
        var hostEndpoint = ParseHostEndpoint(requestExchange.Request.Headers);

        if (hostEndpoint is null)
        {
            _logger.LogDebug("HTTP request is missing a valid Host header.");
            return HypertextTransferProtocolForwardingOutcomes.Failure();
        }

        var outcome = await _forwarder.ForwardAsync(forwardingRequest, hostEndpoint, cancellationToken).ConfigureAwait(false);
        return outcome;
    }

    private async Task<HypertextTransferProtocolForwardingOutcome> GetResponseExchangeAsync(
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        RequestPipelineAction? blockingAction,
        CancellationToken cancellationToken)
    {
        if (blockingAction is RequestPipelineAction.ServeLocalResponse serveAction)
        {
            var localExchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(serveAction.LocalResponse);
            return HypertextTransferProtocolForwardingOutcomes.Standard(localExchange);
        }

        var outcome = await ForwardRequestAsync(forwardingRequest, cancellationToken).ConfigureAwait(false);
        return outcome;
    }

    private async Task HandleBlockedRequestAsync(IProxyConnection connection, TrafficFlow flow, CancellationToken cancellationToken)
    {
        await HypertextTransferProtocolRuleApplicator.SendBlockedResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
        flow.SetResponse(HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData());
        flow.Complete();
        _trafficStore.Add(flow);
        _flowEventPublisher.PublishFlowCompleted(flow);
    }

    private async Task HandleProvisioningRequestAsync(
        IProxyConnection connection,
        TrafficFlow flow,
        HypertextTransferProtocolRequestData request,
        CancellationToken cancellationToken)
    {
        var authority = await _certificateAuthorityProvider!.GetAsync(cancellationToken).ConfigureAwait(false);
        var response = CertificateProvisioningResponder.BuildResponse(request, authority.Certificate);
        await CertificateProvisioningResponder.WriteResponseAsync(connection.Transport.Output, response, cancellationToken).ConfigureAwait(false);
        flow.SetResponse(response);
        _flowEventPublisher.PublishResponseReceived(flow, response);
        flow.Complete();
        _trafficStore.Add(flow);
        _flowEventPublisher.PublishFlowCompleted(flow);
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

    private bool HasDroppedForPacketLoss(TrafficFlow flow)
    {
        if (!ThrottleApplier.HasPacketLossOccurred(_throttleProfile, _packetLossSampler))
        {
            return false;
        }

        FailAndCompleteFlow(flow);
        _trafficStore.Add(flow);
        return true;
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

        finalResponse = ForwardedResponseRewriter.Rewrite(finalResponse);

        var finalExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(responseExchange, finalResponse);
        flow.SetResponse(finalResponse);
        _flowEventPublisher.PublishResponseReceived(flow, finalResponse);
        flow.Complete();
        await ApplyThrottleAsync(cancellationToken).ConfigureAwait(false);
        var downloadBytes = finalExchange.HeaderBytes.Length + finalExchange.Body.Length;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(_throttleProfile, downloadBytes, cancellationToken).ConfigureAwait(false);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(context.Connection.Transport.Output, finalExchange, cancellationToken).ConfigureAwait(false);
        _trafficStore.Add(flow);
        _flowEventPublisher.PublishFlowCompleted(flow);
        return CanKeepClientConnectionAlive(effectiveRequest, finalResponse);
    }

    private async Task<bool> ProcessSingleExchangeAsync(
        IProxyConnection connection,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var flow = CreateTrafficFlow(connection);
        _flowEventPublisher.PublishFlowCreated(flow);
        flow.SetRequest(requestExchange.Request);
        _flowEventPublisher.PublishRequestReceived(flow, requestExchange.Request);
        if (HasDroppedForPacketLoss(flow))
        {
            return false;
        }
        if (_certificateAuthorityProvider is not null
            && CertificateProvisioningResponder.HasProvisioningTarget(requestExchange.Request))
        {
            await HandleProvisioningRequestAsync(connection, flow, requestExchange.Request, cancellationToken).ConfigureAwait(false);
            return false;
        }
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
        effectiveRequest = await ApplyScriptingRequestAsync(flow, effectiveRequest, blockingAction, cancellationToken).ConfigureAwait(false);
        if (blockingAction is not RequestPipelineAction.ServeLocalResponse
            && WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(effectiveRequest))
        {
            return await DispatchUpgradeExchangeAsync(UpgradeExchangeRequestFactory.Create(connection, effectiveRequest, flow, requestExchange), cancellationToken).ConfigureAwait(false);
        }
        var forwardAndProcessRequest = new HypertextTransferProtocolForwardAndProcessRequest
        {
            BlockingAction = blockingAction,
            Connection = connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            RequestExchange = requestExchange,
        };
        return await ForwardAndProcessResponseAsync(forwardAndProcessRequest, cancellationToken).ConfigureAwait(false);
    }
}
