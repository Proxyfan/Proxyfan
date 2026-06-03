using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A TCP proxy listener that binds to a configurable port and accepts incoming connections
///     asynchronously, handing each connection to a caller-supplied <see cref="ConnectionAcceptedHandler" />
///     for further processing.
/// </summary>
public sealed partial class SocketProxyListener : IProxyListener, IDisposable
{
    private readonly ILogger<SocketProxyListener> _logger;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private CancellationTokenSource? _acceptLoopCancellationSource;
    private Task? _acceptLoopTask;
    private SemaphoreSlim? _connectionSemaphore;
    private Socket? _listenSocket;

    /// <summary>
    ///     Initializes a new instance of <see cref="SocketProxyListener" />.
    /// </summary>
    /// <param name="optionsMonitor">
    ///     Live monitor for <see cref="ProxyOptions" /> used to read port and connection settings.
    /// </param>
    /// <param name="logger">
    ///     Logger for structured diagnostic output.
    /// </param>
    public SocketProxyListener(IOptionsMonitor<ProxyOptions> optionsMonitor, ILogger<SocketProxyListener> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public int? BoundPort { get; private set; }

    /// <summary>
    ///     Releases all resources held by this listener.
    /// </summary>
    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
        _listenSocket?.Dispose();
        _acceptLoopCancellationSource?.Dispose();
    }

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    /// <exception cref="ProxyBindException">
    ///     Thrown when the configured port is already in use or access is denied.
    /// </exception>
    public async Task StartAsync(ConnectionAcceptedHandler onConnectionAccepted, CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var port = options.Port;

        var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            socket.DualMode = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var bindEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
            socket.Bind(bindEndPoint);
            socket.Listen();
        }
        catch (SocketException ex)
        {
            socket.Dispose();
            LogBindError(ex, port);
            throw new ProxyBindException(port, ex);
        }

        _listenSocket = socket;
        var connectionSemaphore = new SemaphoreSlim(options.MaxConnections, options.MaxConnections);
        _connectionSemaphore = connectionSemaphore;
        var acceptLoopCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptLoopCancellationSource = acceptLoopCancellationSource;
        IsListening = true;

        if (socket.LocalEndPoint is IPEndPoint boundEndPoint)
        {
            BoundPort = boundEndPoint.Port;
        }

        var listenPort = BoundPort ?? port;
        LogStarted(listenPort);

        _acceptLoopTask = RunAcceptLoopAsync(onConnectionAccepted, _acceptLoopCancellationSource.Token);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// In-flight connections that were accepted before this method is called are allowed to
    /// complete normally rather than being forcibly cancelled. This follows the graceful
    /// shutdown policy described in the design documentation.
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!IsListening)
        {
            return;
        }

        IsListening = false;
        var previousPort = BoundPort;
        BoundPort = null;

        if (_acceptLoopCancellationSource is not null)
        {
            await _acceptLoopCancellationSource.CancelAsync().ConfigureAwait(false);
            _acceptLoopCancellationSource.Dispose();
            _acceptLoopCancellationSource = null;
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

    private async Task HandleConnectionAsync(Socket acceptedSocket, ConnectionAcceptedHandler onConnectionAccepted, CancellationToken cancellationToken)
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
            catch (ObjectDisposedException ex)
            {
                _ = ex;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Error accepting connection on port {Port}")]
    private partial void LogAcceptError(Exception ex, int? port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to bind proxy listener to port {Port}")]
    private partial void LogBindError(Exception ex, int port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Accepted connection from {RemoteEndPoint}")]
    private partial void LogConnectionAccepted(EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled error handling connection from {RemoteEndPoint}")]
    private partial void LogConnectionError(Exception ex, EndPoint remoteEndPoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy listener started on port {Port}")]
    private partial void LogStarted(int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy listener stopped (was on port {Port})")]
    private partial void LogStopped(int? port);

    private async Task RunAcceptLoopAsync(ConnectionAcceptedHandler onConnectionAccepted, CancellationToken cancellationToken)
    {
        var pendingConnections = new List<Task>();

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
                if (AcceptErrorClassifier.HasFatalError(ex, cancellationToken.IsCancellationRequested))
                {
                    break;
                }

                LogAcceptError(ex, BoundPort);
                continue;
            }

            try
            {
                await _connectionSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                acceptedSocket.Dispose();
                break;
            }
            catch (ObjectDisposedException)
            {
                acceptedSocket.Dispose();
                break;
            }

            var connectionTask = HandleConnectionAsync(acceptedSocket, onConnectionAccepted, cancellationToken);
            pendingConnections.Add(connectionTask);
        }

        await Task.WhenAll(pendingConnections).ConfigureAwait(false);
    }
}