using Microsoft.Extensions.Logging;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Domain.Proxy;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Connection handler for SOCKS4 and SOCKS5 connections (RFC 1928 + original SOCKS4 spec).
///     Performs the protocol handshake, opens a TCP tunnel to the requested destination,
///     and relays bytes in both directions until either side closes.
///     Only CONNECT is supported; BIND/UDP ASSOCIATE result in protocol-level failure replies.
///     For SOCKS5 only the No-Authentication method (0x00) is offered to the client.
/// </summary>
public sealed partial class SocksTunnelHandler : IConnectionHandler
{
    private static readonly byte[] Socks5NoAcceptableMethods;
    private static readonly byte[] Socks5NoAuthSelection;
    private readonly UpstreamHostResolver? _hostResolver;
    private readonly ILogger<SocksTunnelHandler> _logger;

    static SocksTunnelHandler()
    {
        Socks5NoAuthSelection = [0x05, 0x00];
        Socks5NoAcceptableMethods = [0x05, 0xFF];
    }

    /// <summary>
    ///     Initializes a new <see cref="SocksTunnelHandler" />.
    /// </summary>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    /// <param name="hostResolver">
    ///     Optional DNS override resolver consulted before dialing the SOCKS5-by-hostname
    ///     destination. When <see langword="null" />, operating-system DNS resolution is used.
    ///     The override does not apply to SOCKS4 / SOCKS5-by-IP destinations because those
    ///     already specify the target as a raw IP address.
    /// </param>
    public SocksTunnelHandler(ILogger<SocksTunnelHandler> logger, UpstreamHostResolver? hostResolver)
    {
        _logger = logger;
        _hostResolver = hostResolver;
    }

    /// <inheritdoc />
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        var version = SocksProtocolDetector.Detect(initialBytes);
        return version is not null;
    }

    /// <inheritdoc />
    public async Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var version = await SocksHandshakeReader.DetectVersionAsync(connection.Transport.Input, cancellationToken).ConfigureAwait(false);

        if (version is null)
        {
            return;
        }

        try
        {
            if (version == SocksVersion.Five)
            {
                await HandleSocks5Async(connection, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await HandleSocks4Async(connection, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogHandshakeError(ex, connection.RemoteEndPoint);
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

    private async Task HandleSocks4Async(IProxyConnection connection, CancellationToken cancellationToken)
    {
        var bytes = await SocksHandshakeReader.ReadIntoArrayAsync(connection.Transport.Input, 9, cancellationToken).ConfigureAwait(false);
        Socks4ConnectRequest? request;

        try
        {
            request = Socks4ConnectRequestParser.TryParse(bytes);
        }
        catch (InvalidDataException ex)
        {
            LogProtocolFailure(ex, connection.RemoteEndPoint);
            await SocksReplyWriter.WriteSocks4ReplyAsync(connection.Transport.Output, isSuccess: false, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (request is null)
        {
            return;
        }

        var totalLength = 8 + request.UserId.Length + 1;
        var result = await connection.Transport.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
        var position = result.Buffer.GetPosition(totalLength);
        connection.Transport.Input.AdvanceTo(position);
        var endpoint = new IPEndPoint(request.DestinationAddress, request.DestinationPort);
        await TunnelToEndpointAsync(connection, endpoint, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleSocks5Async(IProxyConnection connection, CancellationToken cancellationToken)
    {
        Socks5Greeting? greeting;

        try
        {
            greeting = await ReadSocks5GreetingAsync(connection.Transport.Input, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            LogProtocolFailure(ex, connection.RemoteEndPoint);
            await connection.Transport.Output.WriteAsync(Socks5NoAcceptableMethods, cancellationToken).ConfigureAwait(false);
            await connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (greeting is null)
        {
            return;
        }

        if (!SocksHandshakeReader.HasNoAuthMethod(greeting.Methods))
        {
            await connection.Transport.Output.WriteAsync(Socks5NoAcceptableMethods, cancellationToken).ConfigureAwait(false);
            await connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await connection.Transport.Output.WriteAsync(Socks5NoAuthSelection, cancellationToken).ConfigureAwait(false);
        await connection.Transport.Output.FlushAsync(cancellationToken).ConfigureAwait(false);

        Socks5ConnectRequest? request;

        try
        {
            request = await ReadSocks5ConnectRequestAsync(connection.Transport.Input, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            LogProtocolFailure(ex, connection.RemoteEndPoint);
            await SocksReplyWriter.WriteSocks5FailureReplyAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (request is null)
        {
            return;
        }

        await TunnelToHostAsync(connection, request.DestinationAddress, request.DestinationPort, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to connect SOCKS target {Host}:{Port}")]
    private partial void LogConnectFailed(Exception ex, string host, int port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SOCKS handshake error from {RemoteEndPoint}")]
    private partial void LogHandshakeError(Exception ex, EndPoint? remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SOCKS protocol failure from {RemoteEndPoint}")]
    private partial void LogProtocolFailure(Exception ex, EndPoint? remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Trace, Message = "SOCKS relay direction cancelled")]
    private partial void LogRelayCancelled();

    [LoggerMessage(Level = LogLevel.Debug, Message = "SOCKS relay error")]
    private partial void LogRelayError(Exception ex);

    private async Task<Socks5ConnectRequest?> ReadSocks5ConnectRequestAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var bytes = await SocksHandshakeReader.ReadIntoArrayAsync(reader, 10, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < 4)
        {
            return null;
        }

        var request = Socks5ConnectRequestParser.TryParse(bytes);

        if (request is null)
        {
            return null;
        }

        var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var position = result.Buffer.GetPosition(request.TotalLength);
        reader.AdvanceTo(position);
        return request;
    }

    private async Task<Socks5Greeting?> ReadSocks5GreetingAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var bytes = await SocksHandshakeReader.ReadIntoArrayAsync(reader, 2, cancellationToken).ConfigureAwait(false);

        if (bytes.Length < 2)
        {
            return null;
        }

        var greeting = Socks5GreetingParser.TryParse(bytes);

        if (greeting is null)
        {
            return null;
        }

        var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var position = result.Buffer.GetPosition(greeting.TotalLength);
        reader.AdvanceTo(position);
        return greeting;
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

    private async Task TunnelToEndpointAsync(IProxyConnection connection, IPEndPoint endpoint, CancellationToken cancellationToken)
    {
        var tunnelClient = new TcpClient();

        try
        {
            try
            {
                await tunnelClient.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogConnectFailed(ex, endpoint.Address.ToString(), endpoint.Port);
                await SocksReplyWriter.WriteSocks4ReplyAsync(connection.Transport.Output, isSuccess: false, cancellationToken).ConfigureAwait(false);
                return;
            }

            await SocksReplyWriter.WriteSocks4ReplyAsync(connection.Transport.Output, isSuccess: true, cancellationToken).ConfigureAwait(false);
            await RelayAsync(connection, tunnelClient.GetStream(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            tunnelClient.Dispose();
        }
    }

    private async Task TunnelToHostAsync(IProxyConnection connection, string host, int port, CancellationToken cancellationToken)
    {
        var tunnelClient = new TcpClient();

        try
        {
            try
            {
                var effectiveHost = host;
                if (_hostResolver is not null)
                {
                    var canBeIpLiteral = host.Contains('.', StringComparison.Ordinal) || host.Contains(':', StringComparison.Ordinal);
                    if (!canBeIpLiteral || !IPAddress.TryParse(host, out _))
                    {
                        effectiveHost = _hostResolver.Resolve(host);
                    }
                }

                await tunnelClient.ConnectAsync(effectiveHost, port, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogConnectFailed(ex, host, port);
                await SocksReplyWriter.WriteSocks5FailureReplyAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
                return;
            }

            await SocksReplyWriter.WriteSocks5SuccessReplyAsync(connection.Transport.Output, cancellationToken).ConfigureAwait(false);
            await RelayAsync(connection, tunnelClient.GetStream(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            tunnelClient.Dispose();
        }
    }
}
