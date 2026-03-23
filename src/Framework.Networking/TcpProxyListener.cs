using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A TCP proxy listener that binds to a configurable port and accepts incoming connections
///     asynchronously, handing each connection to a caller-supplied callback for further processing.
/// </summary>
public sealed partial class TcpProxyListener(
    IOptionsMonitor<ProxyOptions> optionsMonitor,
    ILogger<TcpProxyListener> logger) : IProxyListener, IDisposable
{
    private readonly ILogger<TcpProxyListener> _logger = logger;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor = optionsMonitor;
    private Task? _acceptLoopTask;
    private CancellationTokenSource? _acceptLoopCts;
    private SemaphoreSlim? _connectionSemaphore;
    private Socket? _listenSocket;

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public int? BoundPort { get; private set; }

    /// <inheritdoc />
    /// <exception cref="ProxyBindException">Thrown when the configured port is already in use or access is denied.</exception>
    public async Task StartAsync(Func<IProxyConnection, CancellationToken, Task> onConnectionAccepted, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var port = options.Port;

        var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            socket.DualMode = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            socket.Listen();
        }
        catch (SocketException ex)
        {
            socket.Dispose();
            LogBindError(ex, port);
            throw new ProxyBindException(port, ex);
        }

        _listenSocket = socket;
        _connectionSemaphore = new SemaphoreSlim(options.MaxConnections, options.MaxConnections);
        _acceptLoopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsListening = true;
        BoundPort = ((IPEndPoint)socket.LocalEndPoint!).Port;

        LogStarted(BoundPort.Value);

        _acceptLoopTask = RunAcceptLoopAsync(onConnectionAccepted, _acceptLoopCts.Token);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     In-flight connections that were accepted before this method is called are allowed to
    ///     complete normally rather than being forcibly cancelled. This follows the graceful
    ///     shutdown policy described in the design documentation.
    /// </remarks>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!IsListening)
        {
            return;
        }

        IsListening = false;
        var previousPort = BoundPort;
        BoundPort = null;

        if (_acceptLoopCts is not null)
        {
            await _acceptLoopCts.CancelAsync().ConfigureAwait(false);
            _acceptLoopCts.Dispose();
            _acceptLoopCts = null;
        }

        if (_acceptLoopTask is not null)
        {
            await _acceptLoopTask.ConfigureAwait(false);
            _acceptLoopTask = null;
        }

        _listenSocket?.Dispose();
        _listenSocket = null;

        _connectionSemaphore?.Dispose();
        _connectionSemaphore = null;

        LogStopped(previousPort);
    }

    /// <summary>Releases all resources held by this listener.</summary>
    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
        _listenSocket?.Dispose();
        _acceptLoopCts?.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to bind proxy listener to port {Port}")]
    private partial void LogBindError(Exception ex, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy listener started on port {Port}")]
    private partial void LogStarted(int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy listener stopped (was on port {Port})")]
    private partial void LogStopped(int? port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error accepting connection on port {Port}")]
    private partial void LogAcceptError(Exception ex, int? port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Accepted connection from {RemoteEndPoint}")]
    private partial void LogConnectionAccepted(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled error handling connection from {RemoteEndPoint}")]
    private partial void LogConnectionError(Exception ex, EndPoint remoteEndPoint);

    private async Task RunAcceptLoopAsync(Func<IProxyConnection, CancellationToken, Task> onConnectionAccepted, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Socket acceptedSocket;

            try
            {
                acceptedSocket = await _listenSocket!.AcceptAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                LogAcceptError(ex, BoundPort);
                continue;
            }

            await _connectionSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);

            _ = HandleConnectionAsync(acceptedSocket, onConnectionAccepted, cancellationToken);
        }
    }

    private async Task HandleConnectionAsync(Socket acceptedSocket, Func<IProxyConnection, CancellationToken, Task> onConnectionAccepted, CancellationToken cancellationToken)
    {
        await using var connection = new SocketConnection(acceptedSocket);

        try
        {
            LogConnectionAccepted(connection.RemoteEndPoint);
            await onConnectionAccepted(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogConnectionError(ex, connection.RemoteEndPoint);
        }
        finally
        {
            try
            {
                _connectionSemaphore?.Release();
            }
            catch (ObjectDisposedException)
            {
                // Semaphore was disposed during shutdown — harmless, ignore.
            }
        }
    }
}
