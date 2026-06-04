using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Rules;
using Proxyfan.Domain.Rules.Pipeline;
using Proxyfan.Domain.Throttling;
using Proxyfan.Domain.Traffic;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Handles HTTP CONNECT requests by either tunneling raw TCP traffic or intercepting
///     HTTPS traffic with transport-layer-security termination for inspection.
///     This class is excluded from code coverage measurement because the bulk of its
///     control flow lives in compiler-generated async state-machine resumption paths
///     (TLS handshake, bidirectional relay, breakpoint awaits) that the source-level
///     coverage tool cannot attribute back to user-written branches. End-to-end
///     behaviour is exercised by
///     <c>TransportLayerSecurityInterceptorHandlerEndToEndTests</c> and the
///     extracted helper types
///     (<c>TransportLayerSecurityInterceptionPipes</c>, <c>DuplexPipeStream</c>,
///     <c>ConnectTargetValidator</c>) are unit-tested independently.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class TransportLayerSecurityInterceptorHandler : IConnectionHandler
{
    private const int MaxHeaderBytes = 65536;
    private const string TunnelErrorResponse = "HTTP/1.1 502 Bad Gateway\r\n\r\n";
    private const string TunnelSuccessResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
    private static readonly byte[] ConnectPrefix;
    private static readonly byte[] ErrorResponseBytes;
    private static readonly byte[] SuccessResponseBytes;
    private readonly TransportLayerSecurityInterceptionContext _context;
    private readonly TransportLayerSecurityInterceptorHandlerDependencies _dependencies;
    private readonly IDomainEventBus _eventBus;
    private readonly UpstreamHostResolver? _hostResolver;
    private readonly ILogger<TransportLayerSecurityInterceptorHandler> _logger;
    private readonly PacketLossSampler _packetLossSampler;
    private readonly IRemoteProcedureCallStore? _remoteProcedureCallStore;
    private readonly IRuleEngine? _ruleEngine;
    private readonly IServerSentEventsStore? _serverSentEventsStore;
    private readonly MutableThrottleProfile? _throttleProfile;
    private readonly TimeProvider _timeProvider;
    private readonly ITrafficStore _trafficStore;

    static TransportLayerSecurityInterceptorHandler()
    {
        var connectPrefixBytes = Encoding.ASCII.GetBytes("CONNECT ");
        var errorBytes = Encoding.ASCII.GetBytes(TunnelErrorResponse);
        var successBytes = Encoding.ASCII.GetBytes(TunnelSuccessResponse);
        ConnectPrefix = connectPrefixBytes;
        ErrorResponseBytes = errorBytes;
        SuccessResponseBytes = successBytes;
    }

    /// <summary>
    ///     Initializes a new <see cref="TransportLayerSecurityInterceptorHandler" /> with bundled dependencies.
    /// </summary>
    /// <param name="dependencies">The bundled handler dependencies.</param>
    public TransportLayerSecurityInterceptorHandler(TransportLayerSecurityInterceptorHandlerDependencies dependencies)
    {
        _dependencies = dependencies;
        _context = dependencies.Context;
        _eventBus = dependencies.EventBus;
        _hostResolver = dependencies.HostResolver;
        _logger = dependencies.Logger;
        _trafficStore = dependencies.TrafficStore;
        _ruleEngine = dependencies.RuleEngine;
        _timeProvider = dependencies.TimeProvider ?? TimeProvider.System;
        _serverSentEventsStore = dependencies.ServerSentEventsStore;
        _remoteProcedureCallStore = dependencies.RemoteProcedureCallStore;
        _throttleProfile = dependencies.ThrottleProfile;
        _packetLossSampler = dependencies.PacketLossSampler ?? DefaultPacketLossSamplers.Shared;
    }

    /// <inheritdoc />
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        if (initialBytes.Length < ConnectPrefix.Length)
        {
            return false;
        }

        Span<byte> prefix = stackalloc byte[ConnectPrefix.Length];
        initialBytes.Slice(0, ConnectPrefix.Length).CopyTo(prefix);
        return prefix.SequenceEqual(ConnectPrefix);
    }

    /// <inheritdoc />
    public async Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var target = await ParseConnectTargetAsync(connection, cancellationToken).ConfigureAwait(false);

        if (target is null || !ConnectTargetValidator.HasValidTarget(target.Host, target.Port))
        {
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            return;
        }

        var strategy = TransportLayerSecurityStrategySelector.Select(_context.ProxyingList, target.Host);

        try
        {
            if (strategy == TransportLayerSecurityHandlingStrategy.InterceptAndInspect)
            {
                await InterceptAsync(connection, target, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await TunnelAsync(connection, target, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (strategy == TransportLayerSecurityHandlingStrategy.InterceptAndInspect)
            {
                LogInterceptError(ex, target.Host, target.Port);
            }
            else
            {
                LogTunnelError(ex, target.Host, target.Port);
            }
        }
    }

    private async Task<bool> CompleteInterceptedResponseAsync(
        TransportLayerSecurityInterceptedForwardContext forwardContext,
        HypertextTransferProtocolProxyResponseExchange responseExchange,
        CancellationToken cancellationToken)
    {
        var context = new TransportLayerSecurityResponsePhaseContext
        {
            EffectiveRequest = forwardContext.EffectiveRequest,
            Flow = forwardContext.Flow,
            Pipes = forwardContext.Pipes,
            ResponseExchange = responseExchange,
        };
        var keepAlive = await ProcessInterceptedResponsePhaseAsync(context, cancellationToken).ConfigureAwait(false);
        return keepAlive;
    }

    private async Task CopyAndSignalAsync(
        PipeReader source,
        PipeWriter destination,
        CancellationTokenSource relayCancellationSource,
        CancellationToken cancellationToken)
    {
        try
        {
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogRelayCancelled();
        }
        catch (Exception ex)
        {
            LogRelayError(ex);
        }
        finally
        {
            await relayCancellationSource.CancelAsync().ConfigureAwait(false);
        }
    }

    private async Task<TcpClient?> DialUpstreamOrFailAsync(IProxyConnection connection, ConnectTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var client = new TcpClient();
            var effectiveHost = _hostResolver is null ? target.Host : _hostResolver.Resolve(target.Host);
            await client.ConnectAsync(effectiveHost, target.Port, cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogConnectFailed(ex, target.Host, target.Port);
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    private async Task DispatchInterceptedServerSentEventsAsync(
        TransportLayerSecurityInterceptedForwardContext forwardContext,
        HypertextTransferProtocolResponseHeaderRead headerRead,
        CancellationToken cancellationToken)
    {
        var streamRequest = new ServerSentEventsStreamRequest
        {
            Connection = forwardContext.Connection,
            EffectiveRequest = forwardContext.EffectiveRequest,
            Flow = forwardContext.Flow,
            ResponseHeaderBytes = headerRead.HeaderBytes,
            ResponseHeaders = headerRead.Response,
            UpstreamPrefetched = [],
            UpstreamStream = forwardContext.Pipes.ServerReader.AsStream(),
        };
        var handler = new ServerSentEventsStreamHandler(
            _eventBus,
            _logger,
            _timeProvider,
            _trafficStore,
            _serverSentEventsStore);
        await handler.HandleAsync(streamRequest, cancellationToken).ConfigureAwait(false);
    }

    private Task DispatchInterceptedUpgradeAsync(
        TransportLayerSecurityInterceptedUpgradeRequest upgradeRequest,
        CancellationToken cancellationToken)
    {
        var dependencies = TransportLayerSecurityInterceptedUpgradeHandlerDependenciesBuilder.Build(_dependencies);
        var upgradeHandler = new TransportLayerSecurityInterceptedUpgradeHandler(dependencies);
        return upgradeHandler.HandleAsync(upgradeRequest, cancellationToken);
    }

    private async Task DispatchVersionTwoAsync(
        IProxyConnection connection,
        SslStream clientSecureStream,
        SslStream serverSecureStream,
        CancellationToken cancellationToken)
    {
        var request = new TransportLayerSecurityInterceptedVersion2DispatchRequest
        {
            ClientSecureStream = clientSecureStream,
            Connection = connection,
            EventBus = _eventBus,
            RemoteProcedureCallStore = _remoteProcedureCallStore,
            ServerSecureStream = serverSecureStream,
            TimeProvider = _timeProvider,
            TrafficStore = _trafficStore,
        };
        await TransportLayerSecurityInterceptedVersion2Dispatch.RunAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private bool HasDroppedForPacketLoss(TrafficFlow flow)
    {
        if (!ThrottleApplier.HasPacketLossOccurred(_throttleProfile, _packetLossSampler))
        {
            return false;
        }

        flow.Fail();
        TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, flow);
        _trafficStore.Add(flow);
        return true;
    }

    private async Task InterceptAsync(IProxyConnection connection, ConnectTarget target, CancellationToken cancellationToken)
    {
        var serverClient = await DialUpstreamOrFailAsync(connection, target, cancellationToken).ConfigureAwait(false);
        if (serverClient is null)
        {
            return;
        }

        using (serverClient)
        {
            await SendSuccessResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            await InterceptWithServerAsync(connection, serverClient, target, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InterceptWithServerAsync(
        IProxyConnection connection,
        TcpClient serverClient,
        ConnectTarget target,
        CancellationToken cancellationToken)
    {
        await using var serverStream = serverClient.GetStream();
        await using var serverSecureStream = new SslStream(serverStream, false);
        var clientOptions = TransportLayerSecurityInterceptorHelpers.CreateClientTransportLayerSecurityOptions(target);
        await serverSecureStream.AuthenticateAsClientAsync(clientOptions, cancellationToken).ConfigureAwait(false);
        var leafCertificate = await _context.GetLeafCertificateAsync(target.Host, cancellationToken).ConfigureAwait(false);
        using var clientStream = new DuplexPipeStream(connection.Transport.Input, connection.Transport.Output);
        await using var clientSecureStream = new SslStream(clientStream, false);
        var serverOptions = TransportLayerSecurityInterceptorHelpers.CreateServerTransportLayerSecurityOptions(leafCertificate, serverSecureStream.NegotiatedApplicationProtocol);
        await clientSecureStream.AuthenticateAsServerAsync(serverOptions, cancellationToken).ConfigureAwait(false);

        if (clientSecureStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
        {
            await DispatchVersionTwoAsync(connection, clientSecureStream, serverSecureStream, cancellationToken).ConfigureAwait(false);
            return;
        }

        var pipes = TransportLayerSecurityInterceptionPipesFactory.Create(clientSecureStream, serverSecureStream);
        var loopContext = new TransportLayerSecurityInterceptedLoopContext
        {
            ClientSecureStream = clientSecureStream,
            Connection = connection,
            Pipes = pipes,
            ServerSecureStream = serverSecureStream,
        };
        try
        {
            await RunHypertextTransferProtocolLoopAsync(loopContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await pipes.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to connect to CONNECT target {Host}:{Port}")]
    private partial void LogConnectFailed(Exception ex, string host, int port);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Unexpected interception error for {Host}:{Port}")]
    private partial void LogInterceptError(Exception ex, string host, int port);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Connection closed before CONNECT headers could be read from {RemoteEndPoint}")]
    private partial void LogNoHeaders(EndPoint? remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Failed to parse CONNECT request from {RemoteEndPoint}")]
    private partial void LogParseError(EndPoint? remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Trace,
        Message = "Relay direction cancelled as expected")]
    private partial void LogRelayCancelled();

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Relay error during CONNECT tunnel")]
    private partial void LogRelayError(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Unexpected tunnel error for {Host}:{Port}")]
    private partial void LogTunnelError(Exception ex, string host, int port);

    private async Task<ConnectTarget?> ParseConnectTargetAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var headerBytes = await PipeReaderHelper
            .ReadUntilEndOfHeadersAsync(connection.Transport.Input, MaxHeaderBytes, cancellationToken)
            .ConfigureAwait(false);

        if (headerBytes is null)
        {
            LogNoHeaders(connection.RemoteEndPoint);
            return null;
        }

        var target = ConnectRequestParser.Parse(headerBytes);

        if (target is null)
        {
            LogParseError(connection.RemoteEndPoint);
        }

        return target;
    }

    private async Task<bool> ProcessInterceptedExchangeAsync(
        TransportLayerSecurityInterceptedLoopContext loopContext,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        var flow = TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow(loopContext.Connection);
        TransportLayerSecurityInterceptorEvents.PublishFlowCreated(_eventBus, flow);
        flow.SetRequest(requestExchange.Request);
        TransportLayerSecurityInterceptorEvents.PublishRequestReceived(_eventBus, flow, requestExchange.Request);

        var flowId = flow.Id.ToString();
        var requestActions = _ruleEngine is not null
            ? await _ruleEngine.EvaluateRequestAsync(requestExchange.Request, flowId, cancellationToken).ConfigureAwait(false)
            : [];
        var effectiveRequest = HypertextTransferProtocolRuleApplicator.ApplyRequestModifications(requestExchange.Request, requestActions);
        var blockingAction = HypertextTransferProtocolRuleApplicator.FindBlockingAction(requestActions);
        if (blockingAction is RequestPipelineAction.Block or RequestPipelineAction.Pause)
        {
            flow.Fail();
            TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, flow);
            return false;
        }

        var serveLocal = blockingAction as RequestPipelineAction.ServeLocalResponse;
        if (serveLocal is null && WebSocketUpgradeDetector.HasWebSocketUpgradeRequest(effectiveRequest))
        {
            var upgradeRequest = new TransportLayerSecurityInterceptedUpgradeRequest
            {
                Context = loopContext,
                EffectiveRequest = effectiveRequest,
                Flow = flow,
                RequestExchange = requestExchange,
            };
            await DispatchInterceptedUpgradeAsync(upgradeRequest, cancellationToken).ConfigureAwait(false);
            return false;
        }

        var forwardContext = new TransportLayerSecurityInterceptedForwardContext
        {
            Connection = loopContext.Connection,
            EffectiveRequest = effectiveRequest,
            Flow = flow,
            Pipes = loopContext.Pipes,
            RequestExchange = requestExchange,
            ServeLocal = serveLocal,
        };
        return await ProcessInterceptedForwardAsync(forwardContext, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> ProcessInterceptedForwardAsync(
        TransportLayerSecurityInterceptedForwardContext forwardContext,
        CancellationToken cancellationToken)
    {
        var pipes = forwardContext.Pipes;
        var effectiveRequest = forwardContext.EffectiveRequest;
        var flow = forwardContext.Flow;
        if (HasDroppedForPacketLoss(flow))
        {
            return false;
        }
        if (forwardContext.ServeLocal is not null)
        {
            var localResponseExchange = HypertextTransferProtocolRuleApplicator.BuildLocalResponseExchange(forwardContext.ServeLocal.LocalResponse);
            return await CompleteInterceptedResponseAsync(forwardContext, localResponseExchange, cancellationToken).ConfigureAwait(false);
        }

        var modifiedExchange = HypertextTransferProtocolRuleApplicator.BuildRequestExchangeWith(forwardContext.RequestExchange, effectiveRequest);
        await WriteRequestToServerAsync(pipes.ServerWriter, modifiedExchange, cancellationToken).ConfigureAwait(false);
        flow.MarkRequestCompleted();

        var headerRead = await HypertextTransferProtocolPipeHelpers.ReadResponseHeadersAsync(pipes.ServerReader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

        if (headerRead is null)
        {
            flow.Fail();
            TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, flow);
            return false;
        }

        flow.MarkResponseStarted();

        if (ServerSentEventsResponseDetector.HasServerSentEventsResponse(headerRead.Response))
        {
            await DispatchInterceptedServerSentEventsAsync(forwardContext, headerRead, cancellationToken).ConfigureAwait(false);
            return false;
        }

        var responseExchange = await HypertextTransferProtocolPipeHelpers.ReadResponseBodyAsync(pipes.ServerReader, headerRead, effectiveRequest.Method, cancellationToken).ConfigureAwait(false);

        if (responseExchange is null)
        {
            flow.Fail();
            TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, flow);
            return false;
        }

        return await CompleteInterceptedResponseAsync(forwardContext, responseExchange, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> ProcessInterceptedResponsePhaseAsync(
        TransportLayerSecurityResponsePhaseContext context,
        CancellationToken cancellationToken)
    {
        var flowId = context.Flow.Id.ToString();
        var responseActions = _ruleEngine is not null
            ? await _ruleEngine.EvaluateResponseAsync(context.EffectiveRequest, context.ResponseExchange.Response, flowId, cancellationToken).ConfigureAwait(false)
            : [];
        if (HypertextTransferProtocolRuleApplicator.HasResponsePauseAction(responseActions))
        {
            context.Flow.Fail();
            TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, context.Flow);
            return false;
        }

        var finalResponse = HypertextTransferProtocolRuleApplicator.ApplyResponseModifications(context.ResponseExchange.Response, responseActions);
        finalResponse = ForwardedResponseRewriter.Rewrite(finalResponse);
        var finalExchange = HypertextTransferProtocolRuleApplicator.BuildResponseExchangeWith(context.ResponseExchange, finalResponse);

        context.Flow.SetResponse(finalResponse);
        TransportLayerSecurityInterceptorEvents.PublishResponseReceived(_eventBus, context.Flow, finalResponse);
        context.Flow.Complete();
        var downloadBytes = finalExchange.HeaderBytes.Length + finalExchange.Body.Length;
        await ThrottleApplier.ApplyDownloadBandwidthAsync(_throttleProfile, downloadBytes, cancellationToken).ConfigureAwait(false);
        await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(context.Pipes.ClientWriter, finalExchange, cancellationToken).ConfigureAwait(false);
        _trafficStore.Add(context.Flow);
        TransportLayerSecurityInterceptorEvents.PublishFlowCompleted(_eventBus, context.Flow);
        return TransportLayerSecurityInterceptorHelpers.HasKeepAlive(context.EffectiveRequest, finalResponse);
    }

    private async Task RelayAsync(IProxyConnection connection, NetworkStream serverStream, CancellationToken cancellationToken)
    {
        using var relayCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var relayToken = relayCancellationSource.Token;
        var serverReader = PipeReader.Create(serverStream);
        var serverWriter = PipeWriter.Create(serverStream);
        var forward = CopyAndSignalAsync(connection.Transport.Input, serverWriter, relayCancellationSource, relayToken);
        var backward = CopyAndSignalAsync(serverReader, connection.Transport.Output, relayCancellationSource, relayToken);
        await Task.WhenAll(forward, backward).ConfigureAwait(false);
    }

    private async Task RunHypertextTransferProtocolLoopAsync(
        TransportLayerSecurityInterceptedLoopContext loopContext,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(loopContext.Pipes.ClientReader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (requestExchange is null)
            {
                break;
            }

            var canContinue = await ProcessInterceptedExchangeAsync(loopContext, requestExchange, cancellationToken).ConfigureAwait(false);

            if (!canContinue)
            {
                break;
            }
        }
    }

    private async Task SendErrorResponseAsync(PipeWriter output, CancellationToken cancellationToken)
    {
        await output.WriteAsync(ErrorResponseBytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SendSuccessResponseAsync(PipeWriter output, CancellationToken cancellationToken)
    {
        await output.WriteAsync(SuccessResponseBytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TunnelAsync(IProxyConnection connection, ConnectTarget target, CancellationToken cancellationToken)
    {
        var tunnelClient = await DialUpstreamOrFailAsync(connection, target, cancellationToken).ConfigureAwait(false);
        if (tunnelClient is null)
        {
            return;
        }

        using (tunnelClient)
        {
            await SendSuccessResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            await RelayAsync(connection, tunnelClient.GetStream(), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteRequestToServerAsync(
        PipeWriter serverWriter,
        HypertextTransferProtocolProxyRequestExchange requestExchange,
        CancellationToken cancellationToken)
    {
        await ThrottleApplier.ApplyLatencyAsync(_throttleProfile, cancellationToken).ConfigureAwait(false);
        var totalBytes = requestExchange.HeaderBytes.Length + requestExchange.Body.Length;
        await ThrottleApplier.ApplyUploadBandwidthAsync(_throttleProfile, totalBytes, cancellationToken).ConfigureAwait(false);
        await serverWriter.WriteAsync(requestExchange.HeaderBytes, cancellationToken).ConfigureAwait(false);
        await serverWriter.WriteAsync(requestExchange.Body, cancellationToken).ConfigureAwait(false);
        await serverWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
