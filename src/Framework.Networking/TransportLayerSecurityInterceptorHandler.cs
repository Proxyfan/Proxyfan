using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Buffers;
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
/// </summary>
public sealed partial class TransportLayerSecurityInterceptorHandler : IConnectionHandler
{
    private const int MaxHeaderBytes = 65536;
    private const string TunnelErrorResponse = "HTTP/1.1 502 Bad Gateway\r\n\r\n";
    private const string TunnelSuccessResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
    private static readonly byte[] ConnectPrefix;
    private static readonly byte[] ErrorResponseBytes;
    private static readonly byte[] SuccessResponseBytes;
    private readonly TransportLayerSecurityInterceptionContext _context;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger<TransportLayerSecurityInterceptorHandler> _logger;
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
    ///     Initializes a new instance of the <see cref="TransportLayerSecurityInterceptorHandler" /> class.
    /// </summary>
    /// <param name="context">The interception context used to resolve certificates and proxying rules.</param>
    /// <param name="trafficStore">The store that persists captured traffic flows.</param>
    /// <param name="eventBus">The domain event bus used to publish captured traffic events.</param>
    /// <param name="logger">The logger used for structured diagnostic output.</param>
    public TransportLayerSecurityInterceptorHandler(
        TransportLayerSecurityInterceptionContext context,
        ITrafficStore trafficStore,
        IDomainEventBus eventBus,
        ILogger<TransportLayerSecurityInterceptorHandler> logger)
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
        _trafficStore = trafficStore;
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

        if (target is null)
        {
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            return;
        }

        var hasProxyingMatch = _context.ProxyingList.HasMatch(target.Host);

        try
        {
            if (hasProxyingMatch)
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
            if (hasProxyingMatch)
            {
                LogInterceptError(ex, target.Host, target.Port);
            }
            else
            {
                LogTunnelError(ex, target.Host, target.Port);
            }
        }
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

    private TransportLayerSecurityInterceptionPipes CreateInterceptionPipes(SslStream clientSecureStream, SslStream serverSecureStream)
    {
        var clientReader = PipeReader.Create(clientSecureStream);
        var clientWriter = PipeWriter.Create(clientSecureStream);
        var serverReader = PipeReader.Create(serverSecureStream);
        var serverWriter = PipeWriter.Create(serverSecureStream);
        var pipes = new TransportLayerSecurityInterceptionPipes(clientReader, clientWriter, serverReader, serverWriter);
        return pipes;
    }

    private async Task InterceptAsync(IProxyConnection connection, ConnectTarget target, CancellationToken cancellationToken)
    {
        TcpClient? serverClient;

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(target.Host, target.Port, cancellationToken).ConfigureAwait(false);
            serverClient = client;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogConnectFailed(ex, target.Host, target.Port);
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
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
        var clientTransportLayerSecurityOptions = TransportLayerSecurityInterceptorHelpers.CreateClientTransportLayerSecurityOptions(target);
        await serverSecureStream.AuthenticateAsClientAsync(clientTransportLayerSecurityOptions, cancellationToken).ConfigureAwait(false);
        var leafCertificate = await _context.GetLeafCertificateAsync(target.Host, cancellationToken).ConfigureAwait(false);
        using var clientStream = new DuplexPipeStream(connection.Transport.Input, connection.Transport.Output);
        await using var clientSecureStream = new SslStream(clientStream, false);
        var serverTransportLayerSecurityOptions = TransportLayerSecurityInterceptorHelpers.CreateServerTransportLayerSecurityOptions(leafCertificate);
        await clientSecureStream.AuthenticateAsServerAsync(serverTransportLayerSecurityOptions, cancellationToken).ConfigureAwait(false);
        var pipes = CreateInterceptionPipes(clientSecureStream, serverSecureStream);

        try
        {
            await RunHypertextTransferProtocolLoopAsync(connection, pipes, cancellationToken).ConfigureAwait(false);
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
        IProxyConnection connection,
        TransportLayerSecurityInterceptionPipes pipes,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipes.ClientReader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (requestExchange is null)
            {
                break;
            }

            var flow = TransportLayerSecurityInterceptorHelpers.CreateTrafficFlow(connection);
            PublishFlowCreated(flow);
            flow.SetRequest(requestExchange.Request);
            PublishRequestReceived(flow, requestExchange.Request);
            await WriteRequestToServerAsync(pipes.ServerWriter, requestExchange, cancellationToken).ConfigureAwait(false);
            var responseExchange = await HypertextTransferProtocolPipeHelpers.ReadResponseAsync(pipes.ServerReader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (responseExchange is null)
            {
                flow.Fail();
                PublishFlowCompleted(flow);
                break;
            }

            flow.SetResponse(responseExchange.Response);
            PublishResponseReceived(flow, responseExchange.Response);
            flow.Complete();
            await HypertextTransferProtocolPipeHelpers.WriteResponseAsync(pipes.ClientWriter, responseExchange, cancellationToken).ConfigureAwait(false);
            _trafficStore.Add(flow);
            PublishFlowCompleted(flow);

            if (!TransportLayerSecurityInterceptorHelpers.HasKeepAlive(requestExchange.Request, responseExchange.Response))
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
        TcpClient? tunnelClient;

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(target.Host, target.Port, cancellationToken).ConfigureAwait(false);
            tunnelClient = client;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogConnectFailed(ex, target.Host, target.Port);
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
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
        await serverWriter.WriteAsync(requestExchange.HeaderBytes, cancellationToken).ConfigureAwait(false);
        await serverWriter.WriteAsync(requestExchange.Body, cancellationToken).ConfigureAwait(false);
        await serverWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
