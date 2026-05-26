using Microsoft.Extensions.Logging;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using Proxyfan.Domain.Traffic.Events;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
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

    private SslClientAuthenticationOptions CreateClientTransportLayerSecurityOptions(ConnectTarget target)
    {
        var options = new SslClientAuthenticationOptions
        {
            TargetHost = target.Host,
        };
        return options;
    }

    private InterceptionPipes CreateInterceptionPipes(SslStream clientSecureStream, SslStream serverSecureStream)
    {
        var clientReader = PipeReader.Create(clientSecureStream);
        var clientWriter = PipeWriter.Create(clientSecureStream);
        var serverReader = PipeReader.Create(serverSecureStream);
        var serverWriter = PipeWriter.Create(serverSecureStream);
        var pipes = new InterceptionPipes(clientReader, clientWriter, serverReader, serverWriter);
        return pipes;
    }

    private SslServerAuthenticationOptions CreateServerTransportLayerSecurityOptions(X509Certificate2 leafCertificate)
    {
        var options = new SslServerAuthenticationOptions
        {
            ClientCertificateRequired = false,
            ServerCertificate = leafCertificate,
        };
        return options;
    }

    private TrafficFlow CreateTrafficFlow(IProxyConnection connection)
    {
        var clientEndPoint = connection.RemoteEndPoint?.ToString();

        if (string.IsNullOrWhiteSpace(clientEndPoint))
        {
            clientEndPoint = "unknown";
        }

        var flow = new TrafficFlow(Guid.NewGuid(), clientEndPoint, DateTimeOffset.UtcNow);
        return flow;
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

    private bool HasKeepAlive(
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
        var clientTransportLayerSecurityOptions = CreateClientTransportLayerSecurityOptions(target);
        await serverSecureStream.AuthenticateAsClientAsync(clientTransportLayerSecurityOptions, cancellationToken).ConfigureAwait(false);
        var leafCertificate = await _context.GetLeafCertificateAsync(target.Host, cancellationToken).ConfigureAwait(false);
        using var clientStream = new DuplexPipeStream(connection.Transport.Input, connection.Transport.Output);
        await using var clientSecureStream = new SslStream(clientStream, false);
        var serverTransportLayerSecurityOptions = CreateServerTransportLayerSecurityOptions(leafCertificate);
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
        InterceptionPipes pipes,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var requestExchange = await HypertextTransferProtocolPipeHelpers.ReadRequestAsync(pipes.ClientReader, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);

            if (requestExchange is null)
            {
                break;
            }

            var flow = CreateTrafficFlow(connection);
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

            if (!HasKeepAlive(requestExchange.Request, responseExchange.Response))
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

    private sealed class DuplexPipeStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public DuplexPipeStream(PipeReader reader, PipeWriter writer)
        {
            var readStream = reader.AsStream();
            var writeStream = writer.AsStream();
            _readStream = readStream;
            _writeStream = writeStream;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
            _writeStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeStream.FlushAsync(cancellationToken);
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _readStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _writeStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class InterceptionPipes
    {
        public PipeReader ClientReader { get; }

        public PipeWriter ClientWriter { get; }

        public PipeReader ServerReader { get; }

        public PipeWriter ServerWriter { get; }

        public InterceptionPipes(
            PipeReader clientReader,
            PipeWriter clientWriter,
            PipeReader serverReader,
            PipeWriter serverWriter)
        {
            ClientReader = clientReader;
            ClientWriter = clientWriter;
            ServerReader = serverReader;
            ServerWriter = serverWriter;
        }

        public Task CompleteAsync(CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            var completionTask = Task.WhenAll(
                ClientReader.CompleteAsync().AsTask(),
                ClientWriter.CompleteAsync().AsTask(),
                ServerReader.CompleteAsync().AsTask(),
                ServerWriter.CompleteAsync().AsTask());
            return completionTask;
        }
    }
}
