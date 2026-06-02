using Microsoft.Extensions.Logging;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Proxy;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Handles HTTP CONNECT requests by establishing a raw TCP tunnel between the client
///     and the target server. Traffic within the tunnel is relayed bidirectionally without
///     inspection or modification. This handler is used for HTTPS destinations that are not
///     on the SSL Proxying List.
/// </summary>
public sealed partial class ConnectTunnelHandler : IConnectionHandler
{
    private const int MaxHeaderBytes = 65536;
    private const string TunnelErrorResponse = "HTTP/1.1 502 Bad Gateway\r\n\r\n";
    private const string TunnelSuccessResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
    private static readonly byte[] ConnectPrefix;
    private static readonly byte[] ErrorResponseBytes;
    private static readonly byte[] SuccessResponseBytes;
    private readonly UpstreamHostResolver? _hostResolver;
    private readonly ILogger<ConnectTunnelHandler> _logger;

    static ConnectTunnelHandler()
    {
        var connectPrefixBytes = Encoding.ASCII.GetBytes("CONNECT ");
        ConnectPrefix = connectPrefixBytes;
        var successBytes = Encoding.ASCII.GetBytes(TunnelSuccessResponse);
        SuccessResponseBytes = successBytes;
        var errorBytes = Encoding.ASCII.GetBytes(TunnelErrorResponse);
        ErrorResponseBytes = errorBytes;
    }

    /// <summary>
    ///     Initializes a new <see cref="ConnectTunnelHandler" />.
    /// </summary>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    /// <param name="hostResolver">
    ///     Optional DNS override resolver consulted before dialing the tunnel target. When
    ///     <see langword="null" />, operating-system DNS resolution is used.
    /// </param>
    public ConnectTunnelHandler(ILogger<ConnectTunnelHandler> logger, UpstreamHostResolver? hostResolver)
    {
        _logger = logger;
        _hostResolver = hostResolver;
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

        try
        {
            await TunnelAsync(connection, target, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogTunnelError(ex, target.Host, target.Port);
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

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to connect to CONNECT target {Host}:{Port}")]
    private partial void LogConnectFailed(Exception ex, string host, int port);

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

    private async Task RelayAsync(IProxyConnection connection, NetworkStream serverStream, CancellationToken cancellationToken)
    {
        using var relayCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var relayToken = relayCancellationSource.Token;
        var serverReader = PipeReader.Create(serverStream);
        var serverWriter = PipeWriter.Create(serverStream);

        var forward = CopyAndSignalAsync(
            connection.Transport.Input, serverWriter, relayCancellationSource, relayToken);
        var backward = CopyAndSignalAsync(
            serverReader, connection.Transport.Output, relayCancellationSource, relayToken);

        await Task.WhenAll(forward, backward).ConfigureAwait(false);
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
        TcpClient? tunnelClient = null;

        var client = new TcpClient();
        try
        {
            var effectiveHost = _hostResolver is null ? target.Host : _hostResolver.Resolve(target.Host);
            await client.ConnectAsync(effectiveHost, target.Port, cancellationToken).ConfigureAwait(false);
            tunnelClient = client;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogConnectFailed(ex, target.Host, target.Port);
            await SendErrorResponseAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (tunnelClient is null)
            {
                client.Dispose();
            }
        }

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
}
