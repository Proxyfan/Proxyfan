using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Forwards an HTTP/1.1 request to the upstream origin (or upstream proxy) and reads the
///     response, branching to a streaming relay when the response is a Server-Sent Events
///     stream. Extracted from <see cref="HypertextTransferProtocolProxyHandler" /> to keep the
///     handler under the analyzer class-size limit and to allow direct unit testing of the
///     forwarding behaviour.
/// </summary>
public sealed class HypertextTransferProtocolForwarder
{
    private const int MaxHeaderBytes = 65536;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IServerSentEventsStore? _serverSentEventsStore;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;
    private readonly IOptionsMonitor<UpstreamProxyOptions>? _upstreamProxy;

    /// <summary>
    ///     Initializes a new <see cref="HypertextTransferProtocolForwarder" />.
    /// </summary>
    /// <param name="dependencies">The bundled forwarder dependencies.</param>
    public HypertextTransferProtocolForwarder(HypertextTransferProtocolForwarderDependencies dependencies)
    {
        _eventBus = dependencies.EventBus;
        _logger = dependencies.Logger;
        _serverSentEventsStore = dependencies.ServerSentEventsStore;
        _timeProvider = dependencies.TimeProvider;
        _trafficStore = dependencies.TrafficStore;
        _upstreamProxy = dependencies.UpstreamProxy;
    }

    /// <summary>
    ///     Forwards the supplied request to the upstream origin (or configured upstream proxy)
    ///     and returns the outcome describing whether to continue with the standard response
    ///     pipeline, that the request was streamed, or that it failed.
    /// </summary>
    /// <param name="forwardingRequest">The forwarding request bundle.</param>
    /// <param name="hostEndpoint">The parsed origin host endpoint.</param>
    /// <param name="cancellationToken">A token that cancels the forwarding operation.</param>
    /// <returns>The forwarding outcome.</returns>
    public async Task<HypertextTransferProtocolForwardingOutcome> ForwardAsync(
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        ConnectTarget hostEndpoint,
        CancellationToken cancellationToken)
    {
        var requestExchange = forwardingRequest.RequestExchange;
        var upstream = ResolveUpstreamRequest(hostEndpoint, requestExchange);
        var upstreamClient = new TcpClient();
        try
        {
            await upstreamClient.ConnectAsync(upstream.Target.Host, upstream.Target.Port, cancellationToken).ConfigureAwait(false);
            var upstreamStream = upstreamClient.GetStream();
            try
            {
                await upstreamStream.WriteAsync(upstream.HeaderBytes, cancellationToken).ConfigureAwait(false);
                await upstreamStream.WriteAsync(requestExchange.Body, cancellationToken).ConfigureAwait(false);
                await upstreamStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                var outcome = await ReadUpstreamResponseAsync(forwardingRequest, upstreamStream, cancellationToken).ConfigureAwait(false);
                return outcome;
            }
            finally
            {
                await upstreamStream.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            upstreamClient.Dispose();
        }
    }

    private async Task<HypertextTransferProtocolForwardingOutcome> ReadUpstreamResponseAsync(
        HypertextTransferProtocolForwardingRequest forwardingRequest,
        NetworkStream upstreamStream,
        CancellationToken cancellationToken)
    {
        var pipeReaderOptions = new StreamPipeReaderOptions(leaveOpen: true);
        var reader = PipeReader.Create(upstreamStream, pipeReaderOptions);
        var headerRead = await HypertextTransferProtocolPipeHelpers
            .ReadResponseHeadersAsync(reader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

        if (headerRead is null)
        {
            await reader.CompleteAsync().ConfigureAwait(false);
            return HypertextTransferProtocolForwardingOutcomes.Failure();
        }

        if (ServerSentEventsResponseDetector.HasServerSentEventsResponse(headerRead.Response))
        {
            var sseRelayRequest = new ServerSentEventsRelayRequest
            {
                ForwardingRequest = forwardingRequest,
                HeaderRead = headerRead,
                Reader = reader,
                UpstreamStream = upstreamStream,
            };
            await RelayServerSentEventsAsync(sseRelayRequest, cancellationToken).ConfigureAwait(false);
            return HypertextTransferProtocolForwardingOutcomes.Streamed();
        }

        var method = forwardingRequest.RequestExchange.Request.Method;
        var exchange = await HypertextTransferProtocolPipeHelpers
            .ReadResponseBodyAsync(reader, headerRead, method, cancellationToken).ConfigureAwait(false);
        await reader.CompleteAsync().ConfigureAwait(false);

        if (exchange is null)
        {
            return HypertextTransferProtocolForwardingOutcomes.Failure();
        }

        return HypertextTransferProtocolForwardingOutcomes.Standard(exchange);
    }

    private async Task RelayServerSentEventsAsync(
        ServerSentEventsRelayRequest relayRequest,
        CancellationToken cancellationToken)
    {
        var forwardingRequest = relayRequest.ForwardingRequest;
        var headerRead = relayRequest.HeaderRead;
        var reader = relayRequest.Reader;
        var upstreamStream = relayRequest.UpstreamStream;
        var prefetched = await PipeReaderDrainer.DrainBufferedBytesAsync(reader, cancellationToken).ConfigureAwait(false);
        await reader.CompleteAsync().ConfigureAwait(false);
        var sseHandler = new ServerSentEventsStreamHandler(_eventBus, _logger, _timeProvider, _trafficStore, _serverSentEventsStore);
        var sseRequest = new ServerSentEventsStreamRequest
        {
            Connection = forwardingRequest.Connection,
            EffectiveRequest = forwardingRequest.EffectiveRequest,
            Flow = forwardingRequest.Flow,
            ResponseHeaders = headerRead.Response,
            ResponseHeaderBytes = headerRead.HeaderBytes,
            UpstreamStream = upstreamStream,
            UpstreamPrefetched = prefetched,
        };
        await sseHandler.HandleAsync(sseRequest, cancellationToken).ConfigureAwait(false);
    }

    private UpstreamForwardingTarget ResolveUpstreamRequest(
        ConnectTarget hostEndpoint,
        HypertextTransferProtocolProxyRequestExchange requestExchange)
    {
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
            : OriginRequestRewriter.RewriteHeaders(requestExchange.HeaderBytes, requestExchange.Request);
        var target = new UpstreamForwardingTarget
        {
            HeaderBytes = headerBytes,
            Target = connectTarget,
        };
        return target;
    }
}
