using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Traffic;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Orchestrates the HTTP/1.1 Upgrade handshake (e.g. WebSocket): connects to the upstream
///     origin, forwards the request, reads the 101 response, and starts a bidirectional tunnel
///     when the upgrade succeeds. Extracted from <see cref="HypertextTransferProtocolProxyHandler" />
///     to keep the handler below the analyzer's class-size limit and to allow direct unit
///     testing of the upgrade orchestration.
/// </summary>
public sealed class HypertextTransferProtocolUpgradeOrchestrator
{
    private const int MaxHeaderBytes = 65536;
    private readonly HypertextTransferProtocolFlowEventPublisher _flowEventPublisher;
    private readonly UpstreamHostResolver? _hostResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;
    private readonly IWebSocketStore? _webSocketStore;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolUpgradeOrchestrator" />.
    /// </summary>
    /// <param name="dependencies">The bundled orchestrator dependencies.</param>
    public HypertextTransferProtocolUpgradeOrchestrator(HypertextTransferProtocolUpgradeOrchestratorDependencies dependencies)
    {
        _flowEventPublisher = dependencies.FlowEventPublisher;
        _hostResolver = dependencies.HostResolver;
        _timeProvider = dependencies.TimeProvider;
        _trafficStore = dependencies.TrafficStore;
        _webSocketStore = dependencies.WebSocketStore;
    }

    /// <summary>
    ///     Connects to the upstream origin identified by <paramref name="hostEndpoint" />,
    ///     forwards the upgrade request, and either tunnels the WebSocket exchange or completes
    ///     the flow with a non-101 response. The flow is marked failed and false is returned
    ///     when the upstream cannot be reached or returns an unparsable response.
    /// </summary>
    /// <param name="request">The upgrade exchange request.</param>
    /// <param name="hostEndpoint">The upstream origin endpoint parsed from the Host header.</param>
    /// <param name="cancellationToken">A token that cancels the orchestration.</param>
    /// <returns><see langword="false" /> always â€” the client connection must not be reused.</returns>
    public async Task<bool> DispatchAsync(
        UpgradeExchangeRequest request,
        ConnectTarget hostEndpoint,
        CancellationToken cancellationToken)
    {
        var upstreamClient = new TcpClient();
        try
        {
            var effectiveHost = _hostResolver is null ? hostEndpoint.Host : _hostResolver.Resolve(hostEndpoint.Host);
            await upstreamClient.ConnectAsync(effectiveHost, hostEndpoint.Port, cancellationToken).ConfigureAwait(false);
            var upstreamStream = upstreamClient.GetStream();
            await SendUpgradeRequestUpstreamAsync(request, upstreamStream, cancellationToken).ConfigureAwait(false);
            var upstreamUpgradeResponse = await ReadUpgradeResponseFromUpstreamAsync(request, upstreamStream, cancellationToken).ConfigureAwait(false);

            if (upstreamUpgradeResponse is null)
            {
                FailFlow(request.Flow);
                return false;
            }

            await CompleteUpgradeExchangeAsync(request, upstreamStream, upstreamUpgradeResponse, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            upstreamClient.Dispose();
        }

        return false;
    }

    private void CompleteFlow(TrafficFlow flow)
    {
        flow.Complete();
        _trafficStore.Add(flow);
        _flowEventPublisher.PublishFlowCompleted(flow);
    }

    private async Task CompleteUpgradeExchangeAsync(
        UpgradeExchangeRequest request,
        NetworkStream upstreamStream,
        UpgradeResponseExchange upstreamUpgradeResponse,
        CancellationToken cancellationToken)
    {
        var upstreamResponseExchange = upstreamUpgradeResponse.ResponseExchange;
        var rewrittenResponse = UpgradeResponseRewriter.Rewrite(upstreamResponseExchange.Response);
        var clientFacingExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(upstreamResponseExchange, rewrittenResponse);
        request.Flow.SetResponse(rewrittenResponse);
        _flowEventPublisher.PublishResponseReceived(request.Flow, rewrittenResponse);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(request.Connection.Transport.Output, clientFacingExchange, cancellationToken).ConfigureAwait(false);

        if (!WebSocketUpgradeDetector.HasWebSocketUpgradeSuccess(request.EffectiveRequest, rewrittenResponse))
        {
            CompleteFlow(request.Flow);
            return;
        }

        var webSocketFlow = new WebSocketFlow(request.Flow);
        _webSocketStore?.Add(webSocketFlow);
        var upstreamReadWriteStream = ResolveUpstreamStream(upstreamStream, upstreamUpgradeResponse);
        var clientStream = new DuplexPipeStream(request.Connection.Transport.Input, request.Connection.Transport.Output);
        var tunnel = new WebSocketUpgradeTunnel(_timeProvider);

        try
        {
            await tunnel.TunnelAsync(clientStream, upstreamReadWriteStream, webSocketFlow, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CompleteFlow(request.Flow);
        }
    }

    private void FailFlow(TrafficFlow flow)
    {
        flow.Fail();
        _flowEventPublisher.PublishFlowCompleted(flow);
    }

    private async Task<UpgradeResponseExchange?> ReadUpgradeResponseFromUpstreamAsync(
        UpgradeExchangeRequest request,
        NetworkStream upstreamStream,
        CancellationToken cancellationToken)
    {
        var pipeReaderOptions = new StreamPipeReaderOptions(leaveOpen: true);
        var upstreamReader = PipeReader.Create(upstreamStream, pipeReaderOptions);
        var upstreamResponseExchange = await HypertextTransferProtocolPipeHelpers
            .ReadResponseAsync(upstreamReader, MaxHeaderBytes, request.EffectiveRequest.Method, cancellationToken).ConfigureAwait(false);

        if (upstreamResponseExchange is null)
        {
            await upstreamReader.CompleteAsync().ConfigureAwait(false);
            return null;
        }

        var prefetched = await PipeReaderDrainer.DrainBufferedBytesAsync(upstreamReader, cancellationToken).ConfigureAwait(false);
        await upstreamReader.CompleteAsync().ConfigureAwait(false);
        var upgradeExchange = new UpgradeResponseExchange(upstreamResponseExchange, prefetched);
        return upgradeExchange;
    }

    private Stream ResolveUpstreamStream(NetworkStream upstreamStream, UpgradeResponseExchange upstreamUpgradeResponse)
    {
        if (upstreamUpgradeResponse.PrefetchedBytes.Length == 0)
        {
            return upstreamStream;
        }

        var prefixedUpstreamStream = new PrefixedReadStream(upstreamUpgradeResponse.PrefetchedBytes, upstreamStream);
        return prefixedUpstreamStream;
    }

    private async Task SendUpgradeRequestUpstreamAsync(
        UpgradeExchangeRequest request,
        NetworkStream upstreamStream,
        CancellationToken cancellationToken)
    {
        var rebuiltRequestExchange = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(request.RequestExchange, request.EffectiveRequest);
        var upstreamHeaderBytes = UpgradeRequestRewriter.RewriteHeaders(rebuiltRequestExchange.HeaderBytes, request.EffectiveRequest);
        await upstreamStream.WriteAsync(upstreamHeaderBytes, cancellationToken).ConfigureAwait(false);
        await upstreamStream.WriteAsync(rebuiltRequestExchange.Body, cancellationToken).ConfigureAwait(false);
        await upstreamStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
