using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Per-connection HTTP/1.1 request/response handler for reverse-proxy routes. Reads HTTP
///     requests from the client side, evaluates the configured rule pipeline (Block / Map
///     Local / Modify) against them, rewrites the <c>Host</c> header to point at the route's
///     backend, forwards the request to the backend using the shared
///     <see cref="HypertextTransferProtocolForwarder" /> (so SSE / chunked / framing behaviour
///     matches the forward proxy), evaluates response rules, and writes the captured response
///     back to the client. Captured flows are added to the shared
///     <see cref="ITrafficStore" /> so reverse-proxy traffic appears in the same inspector as
///     forward-proxy traffic.
/// </summary>
public sealed class ReverseProxyHypertextTransferProtocolHandler
{
    private const int MaxHeaderBytes = 65536;
    private readonly HypertextTransferProtocolFlowEventPublisher _flowEventPublisher;
    private readonly HypertextTransferProtocolForwarder _forwarder;
    private readonly ILogger<ReverseProxyHypertextTransferProtocolHandler> _logger;
    private readonly IRuleEngine _ruleEngine;
    private readonly ITrafficStore _trafficStore;

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxyHypertextTransferProtocolHandler" />.
    /// </summary>
    /// <param name="dependencies">The bundled handler dependencies.</param>
    public ReverseProxyHypertextTransferProtocolHandler(ReverseProxyHypertextTransferProtocolHandlerDependencies dependencies)
    {
        _logger = dependencies.Logger;
        _ruleEngine = dependencies.RuleEngine;
        _trafficStore = dependencies.TrafficStore;
        var publisher = new HypertextTransferProtocolFlowEventPublisher(dependencies.EventBus);
        _flowEventPublisher = publisher;
        var forwarderDependencies = new HypertextTransferProtocolForwarderDependencies
        {
            EventBus = dependencies.EventBus,
            HostResolver = null,
            Logger = _logger,
            ServerSentEventsStore = null,
            ThrottleProfile = null,
            TimeProvider = dependencies.TimeProvider,
            TrafficStore = _trafficStore,
            UpstreamProxy = null,
        };
        var forwarder = new HypertextTransferProtocolForwarder(forwarderDependencies);
        _forwarder = forwarder;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied initial bytes look like the start
    ///     of an HTTP/1.1 request line (a method token followed by a space).
    /// </summary>
    /// <param name="initialBytes">The buffered initial bytes from the connection.</param>
    /// <returns>True when the bytes are HTTP-shaped.</returns>
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        return HypertextTransferProtocolMethodPrefixDetector.HasMethodPrefix(initialBytes);
    }

    /// <summary>
    ///     Reads HTTP requests from the client side of <paramref name="connection" /> in a
    ///     loop, forwards each to the configured backend, and writes the response back. The
    ///     loop exits when the client closes, the response indicates <c>Connection: close</c>,
    ///     or the cancellation token is signalled.
    /// </summary>
    /// <param name="connection">The accepted client connection wrapped as an <see cref="IProxyConnection" />.</param>
    /// <param name="route">The reverse proxy route this handler is serving.</param>
    /// <param name="cancellationToken">A token that cancels the handling loop.</param>
    /// <returns>A task that completes when the loop exits.</returns>
    public async Task HandleAsync(
        IProxyConnection connection,
        ReverseProxyRoute route,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers
                .ReadRequestAsync(connection.Transport.Input, MaxHeaderBytes, cancellationToken)
                .ConfigureAwait(false);

            if (requestExchange is null)
            {
                return;
            }

            var canContinue = await ProcessSingleExchangeAsync(connection, route, requestExchange, cancellationToken)
                .ConfigureAwait(false);

            if (!canContinue)
            {
                return;
            }
        }
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
        _flowEventPublisher.PublishFlowCompleted(flow);
    }

    private async Task<HypertextTransferProtocolForwardingOutcome> ForwardToBackendAsync(
        ReverseProxyRoute route,
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        RequestPipelineAction? blockingAction,
        CancellationToken cancellationToken)
    {
        if (blockingAction is RequestPipelineAction.ServeLocalResponse serveAction)
        {
            var localExchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(serveAction.LocalResponse);
            return HypertextTransferProtocolForwardingOutcomes.Standard(localExchange);
        }

        var backendTarget = new ConnectTarget(route.BackendHost, route.BackendPort);
        try
        {
            var outcome = await _forwarder.ForwardAsync(forwardingRequest, backendTarget, cancellationToken).ConfigureAwait(false);
            return outcome;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Reverse proxy backend forward failed for route {RouteIdentifier}", route.Identifier);
            return HypertextTransferProtocolForwardingOutcomes.Failure();
        }
    }

    private bool HasCanKeepClientConnectionAlive(
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

    private bool HasConnectionCloseDirective(HeaderCollection headers)
    {
        var connectionValue = headers.Get("Connection");

        if (string.IsNullOrWhiteSpace(connectionValue))
        {
            return false;
        }

        return connectionValue.Contains("close", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ProcessSingleExchangeAsync(
        IProxyConnection connection,
        ReverseProxyRoute route,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var flow = CreateTrafficFlow(connection);
        _flowEventPublisher.PublishFlowCreated(flow);
        flow.SetRequest(requestExchange.Request);
        _flowEventPublisher.PublishRequestReceived(flow, requestExchange.Request);

        var requestActions = _ruleEngine.EvaluateRequest(requestExchange.Request);
        var effectiveRequest = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(requestExchange.Request, requestActions);
        var blockingAction = HypertextTransferProtocolRuleApplicator.FindBlockingAction(requestActions);

        if (blockingAction is RequestPipelineAction.Block)
        {
            await HypertextTransferProtocolRuleApplicator.SendBlockedResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            flow.SetResponse(HypertextTransferProtocolRuleApplicator.CreateBlockedResponseData());
            flow.Complete();
            _trafficStore.Add(flow);
            _flowEventPublisher.PublishFlowCompleted(flow);
            return false;
        }

        effectiveRequest = ReverseProxyHostHeaderRewriter.Rewrite(effectiveRequest, route.BackendHost, route.BackendPort);
        var requestForUpstream = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(requestExchange, effectiveRequest);
        var forwardingRequest = new HypertextTransferProtocolForwardingRequest
        {
            Connection = connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            RequestExchange = requestForUpstream,
        };

        var outcome = await ForwardToBackendAsync(route, forwardingRequest, blockingAction, cancellationToken).ConfigureAwait(false);

        if (outcome.IsFailure)
        {
            FailAndCompleteFlow(flow);
            return false;
        }

        if (outcome.IsStreaming)
        {
            return false;
        }

        return await WriteResponseAsync(forwardingRequest, outcome.Exchange!, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> WriteResponseAsync(
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        HypertextTransferProtocolProxyResponseExchange responseExchange,
        CancellationToken cancellationToken)
    {
        var connection = forwardingRequest.Connection;
        var flow = forwardingRequest.Flow;
        var effectiveRequest = forwardingRequest.EffectiveRequest;
        var responseActions = _ruleEngine.EvaluateResponse(effectiveRequest, responseExchange.Response);
        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(responseExchange.Response, responseActions);
        finalResponse = ForwardedResponseRewriter.Rewrite(finalResponse);
        var finalExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(responseExchange, finalResponse);

        flow.SetResponse(finalResponse);
        _flowEventPublisher.PublishResponseReceived(flow, finalResponse);
        flow.Complete();

        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(connection.Transport.Output, finalExchange, cancellationToken).ConfigureAwait(false);
        _trafficStore.Add(flow);
        _flowEventPublisher.PublishFlowCompleted(flow);

        return HasCanKeepClientConnectionAlive(effectiveRequest, finalResponse);
    }
}
