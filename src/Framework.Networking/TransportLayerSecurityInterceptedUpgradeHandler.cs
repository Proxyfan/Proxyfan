using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
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
///     writes the rewritten response back to the client, drains client-side prefetched
///     bytes (the client may have sent the first WebSocket frame immediately after the
///     handshake), then runs the <see cref="WebSocketUpgradeTunnel" /> on the underlying
///     TLS streams with <see cref="PrefixedReadStream" /> wrappers so no prefetched bytes
///     are lost. The bidirectional tunnel runs until either side closes.
/// </summary>
public sealed class TransportLayerSecurityInterceptedUpgradeHandler
{
    private const int MaxHeaderBytes = 65536;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;
    private readonly IWebSocketStore? _webSocketStore;

    /// <summary>
    ///     Initializes a new <see cref="TransportLayerSecurityInterceptedUpgradeHandler" />.
    /// </summary>
    /// <param name="eventBus">The domain event bus used to publish flow events.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider used for WebSocket message timestamps.</param>
    /// <param name="trafficStore">The traffic store that retains completed flows.</param>
    /// <param name="webSocketStore">The optional WebSocket store that retains captured WebSocket messages.</param>
    public TransportLayerSecurityInterceptedUpgradeHandler(
        IDomainEventBus eventBus,
        ILogger logger,
        TimeProvider timeProvider,
        ITrafficStore trafficStore,
        IWebSocketStore? webSocketStore)
    {
        _eventBus = eventBus;
        _logger = logger;
        _timeProvider = timeProvider;
        _trafficStore = trafficStore;
        _webSocketStore = webSocketStore;
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
        var rewrittenResponse = UpgradeResponseRewriter.Rewrite(upstreamResponse.Response);
        var clientFacingExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(upstreamResponse, rewrittenResponse);
        request.Flow.SetResponse(rewrittenResponse);
        PublishResponseReceived(request.Flow, rewrittenResponse);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(pipes.ClientWriter, clientFacingExchange, cancellationToken).ConfigureAwait(false);

        if (!WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request.EffectiveRequest, rewrittenResponse))
        {
            request.Flow.Complete();
            _trafficStore.Add(request.Flow);
            PublishFlowCompleted(request.Flow);
            return;
        }

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
            request.Flow.Complete();
            _trafficStore.Add(request.Flow);
            PublishFlowCompleted(request.Flow);
        }
    }

    private void FailFlow(TrafficFlow flow)
    {
        flow.Fail();
        var completedEvent = new TrafficFlowCompleted(flow.Id, flow.Status, DateTimeOffset.UtcNow);
        _eventBus.Publish(completedEvent);
        _logger.LogDebug("TLS-intercepted WebSocket upgrade failed: no upstream response.");
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
}
