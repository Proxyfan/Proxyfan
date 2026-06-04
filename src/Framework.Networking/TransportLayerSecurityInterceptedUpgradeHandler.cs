using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Orchestrates an HTTP/1.1 Upgrade exchange that originated inside the TLS-intercepted
///     loop (wss:// upgrades, HTTP/2 upgrade-over-TLS, custom Upgrade). Sends the rewritten
///     upgrade request, reads the upstream response (preserving any prefetched bytes the
///     <see cref="System.IO.Pipelines.PipeReader" /> consumed past the response headers),
///     routes the upstream response through the same response-phase pipeline as a normal
///     intercepted HTTPS response (rule engine, scripting hook, breakpoint), writes the
///     resulting response back to the client, drains client-side prefetched bytes (the
///     client may have sent the first WebSocket frame immediately after the handshake),
///     then runs the <see cref="WebSocketUpgradeTunnel" /> on the underlying TLS streams
///     with <see cref="PrefixedReadStream" /> wrappers so no prefetched bytes are lost.
///     The bidirectional tunnel runs until either side closes. Applies to both 101
///     Switching Protocols responses and non-101 rejected upgrades, so policies behave
///     identically for TLS upgrades and normal HTTPS responses.
/// </summary>
public sealed class TransportLayerSecurityInterceptedUpgradeHandler
{
    private const int MaxHeaderBytes = 65536;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IRuleEngine? _ruleEngine;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;
    private readonly IWebSocketStore? _webSocketStore;

    /// <summary>
    ///     Initializes a new <see cref="TransportLayerSecurityInterceptedUpgradeHandler" />.
    /// </summary>
    /// <param name="dependencies">The bundled handler dependencies.</param>
    public TransportLayerSecurityInterceptedUpgradeHandler(
        TransportLayerSecurityInterceptedUpgradeHandlerDependencies dependencies)
    {
        _eventBus = dependencies.EventBus;
        _logger = dependencies.Logger;
        _timeProvider = dependencies.TimeProvider;
        _trafficStore = dependencies.TrafficStore;
        _webSocketStore = dependencies.WebSocketStore;
        _ruleEngine = dependencies.RuleEngine;
    }

    /// <summary>
    ///     Executes the upgrade exchange and tunnels frames bidirectionally until either side
    ///     closes the underlying TLS streams.
    /// </summary>
    /// <param name="request">The upgrade request bundle.</param>
    /// <param name="cancellationToken">A token that cancels the upgrade and tunneling.</param>
    /// <returns>A task that completes when the tunnel terminates.</returns>
    public async Task HandleAsync(
        TransportLayerSecurityInterceptedUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var pipes = request.Context.Pipes;
        var upstreamHeaderBytes = UpgradeRequestRewriter.RewriteHeaders(request.RequestExchange.HeaderBytes, request.EffectiveRequest);
        await pipes.ServerWriter.WriteAsync(upstreamHeaderBytes, cancellationToken).ConfigureAwait(false);
        await pipes.ServerWriter.WriteAsync(request.RequestExchange.Body, cancellationToken).ConfigureAwait(false);
        await pipes.ServerWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        var upstreamResponse = await HypertextTransferProtocolPipeHelpers
            .ReadResponseAsync(pipes.ServerReader, MaxHeaderBytes, request.EffectiveRequest.Method, cancellationToken)
            .ConfigureAwait(false);

        if (upstreamResponse is null)
        {
            FailFlow(request.Flow);
            return;
        }

        var serverPrefetched = await PipeReaderDrainer.DrainBufferedBytesAsync(pipes.ServerReader, cancellationToken).ConfigureAwait(false);
        var policyResponse = await ApplyResponsePoliciesAsync(request, upstreamResponse.Response, cancellationToken).ConfigureAwait(false);

        if (policyResponse is null)
        {
            FailFlow(request.Flow);
            return;
        }

        var rewrittenResponse = UpgradeResponseRewriter.Rewrite(policyResponse);
        var clientFacingExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(upstreamResponse, rewrittenResponse);
        request.Flow.SetResponse(rewrittenResponse);
        PublishResponseReceived(request.Flow, rewrittenResponse);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(pipes.ClientWriter, clientFacingExchange, cancellationToken).ConfigureAwait(false);

        if (!WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request.EffectiveRequest, rewrittenResponse))
        {
            CompleteFlow(request.Flow);
            return;
        }

        await TunnelWebSocketAsync(request, serverPrefetched, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HypertextTransferProtocolResponseData?> ApplyResponsePoliciesAsync(
        TransportLayerSecurityInterceptedUpgradeRequest request,
        HypertextTransferProtocolResponseData upstreamResponse,
        CancellationToken cancellationToken)
    {
        var flowId = request.Flow.Id.ToString();
        var responseActions = _ruleEngine is not null
            ? await _ruleEngine.EvaluateResponseAsync(request.EffectiveRequest, upstreamResponse, flowId, cancellationToken).ConfigureAwait(false)
            : [];
        if (HypertextTransferProtocolRuleApplicator.HasResponsePauseAction(responseActions))
        {
            return null;
        }

        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(upstreamResponse, responseActions);
        return finalResponse;
    }

    private void CompleteFlow(TrafficFlow flow)
    {
        flow.Complete();
        _trafficStore.Add(flow);
        PublishFlowCompleted(flow);
    }

    private void FailFlow(TrafficFlow flow)
    {
        flow.Fail();
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
        _logger.LogDebug("TLS-intercepted WebSocket upgrade failed: no upstream response or response aborted by breakpoint.");
    }

    private void PublishFlowCompleted(TrafficFlow flow)
    {
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
    }

    private void PublishResponseReceived(TrafficFlow flow, HypertextTransferProtocolResponseData response)
    {
        var responseReceivedEvent = new ResponseReceived(flow.Id, response, DateTimeOffset.UtcNow);
        _eventBus.Publish(responseReceivedEvent);
    }

    private async Task TunnelWebSocketAsync(
        TransportLayerSecurityInterceptedUpgradeRequest request,
        byte[] serverPrefetched,
        CancellationToken cancellationToken)
    {
        var pipes = request.Context.Pipes;
        var clientPrefetched = await PipeReaderDrainer.DrainBufferedBytesAsync(pipes.ClientReader, cancellationToken).ConfigureAwait(false);
        await pipes.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var webSocketFlow = new WebSocketFlow(request.Flow);
        _webSocketStore?.Add(webSocketFlow);

        var clientStream = UpgradePrefixedStreamFactory.WrapWithPrefix(clientPrefetched, request.Context.ClientSecureStream);
        var serverStream = UpgradePrefixedStreamFactory.WrapWithPrefix(serverPrefetched, request.Context.ServerSecureStream);
        var tunnel = new WebSocketUpgradeTunnel(_timeProvider);

        try
        {
            await tunnel.TunnelAsync(clientStream, serverStream, webSocketFlow, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CompleteFlow(request.Flow);
        }
    }
}
