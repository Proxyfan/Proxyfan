using Microsoft.Extensions.Logging;
using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A reverse proxy route listener: binds a TCP port and for each accepted client
///     connection opens a parallel connection to the configured backend and pumps bytes
///     bidirectionally until either side closes.
/// </summary>
public sealed partial class ReverseProxyRouteListener : IDisposable
{
    private const int BufferSize = 16384;
    private readonly ILogger<ReverseProxyRouteListener> _logger;
    private readonly List<Task> _pendingForwards;
    private readonly ReverseProxyRoute _route;
    private Task? _acceptTask;
    private CancellationTokenSource? _cancellationSource;
    private TcpListener? _listener;

    /// <summary>
    ///     Gets a value indicating whether the listener is currently bound and accepting.
    /// </summary>
    public bool IsListening { get; private set; }

    /// <summary>
    ///     Initializes a new listener for the given <paramref name="route" />.
    /// </summary>
    /// <param name="route">The route to bind and forward.</param>
    /// <param name="logger">The diagnostic logger.</param>
    public ReverseProxyRouteListener(ReverseProxyRoute route, ILogger<ReverseProxyRouteListener> logger)
    {
        _route = route;
        _logger = logger;
        _pendingForwards = [];
    }

    /// <summary>
    ///     Disposes the listener and stops accepting.
    /// </summary>
    public void Dispose()
    {
        _cancellationSource?.Dispose();
        _listener?.Dispose();
    }

    /// <summary>
    ///     Returns the route configuration this listener wraps.
    /// </summary>
    /// <returns>The route configuration.</returns>
    public ReverseProxyRoute GetRoute()
    {
        return _route;
    }

    /// <summary>
    ///     Starts accepting on the route's listen port.
    /// </summary>
    /// <param name="cancellationToken">Cancels start-up.</param>
    /// <returns>A task that completes once the listener is bound.</returns>
    /// <exception cref="ProxyBindException">Thrown if the bind fails.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, _route.ListenPort);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            LogBindError(ex, _route.ListenPort);
            throw new ProxyBindException(_route.ListenPort, ex);
        }

        _listener = listener;
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationSource = source;
        IsListening = true;
        LogStarted(_route.Identifier, _route.ListenPort, _route.BackendHost, _route.BackendPort);
        _acceptTask = RunAcceptLoopAsync(_cancellationSource.Token);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    ///     Stops accepting connections.
    /// </summary>
    /// <param name="cancellationToken">Cancels shutdown.</param>
    /// <returns>A task that completes when the accept loop exits.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (!IsListening)
        {
            return;
        }

        IsListening = false;
        if (_cancellationSource is not null)
        {
            await _cancellationSource.CancelAsync().ConfigureAwait(false);
            _cancellationSource.Dispose();
            _cancellationSource = null;
        }

        _listener?.Stop();
        _listener = null;

        if (_acceptTask is not null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _ = ex;
            }

            _acceptTask = null;
        }

        Task[] pending;
        lock (_pendingForwards)
        {
            pending = [.. _pendingForwards];
            _pendingForwards.Clear();
        }

        if (pending.Length > 0)
        {
            try
            {
                await Task.WhenAll(pending).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ = ex;
            }
        }

        LogStopped(_route.Identifier);
    }

    private async Task ForwardConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            using var backend = new TcpClient();
            try
            {
                await backend.ConnectAsync(_route.BackendHost, _route.BackendPort, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                LogBackendConnectError(ex, _route.BackendHost, _route.BackendPort);
                return;
            }

            using var clientStream = client.GetStream();
            using var backendStream = backend.GetStream();
            await BidirectionalStreamPump.PumpAsync(clientStream, backendStream, BufferSize, cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reverse proxy route failed to connect to backend {Host}:{Port}")]
    private partial void LogBackendConnectError(Exception ex, string host, int port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reverse proxy route failed to bind to listen port {Port}")]
    private partial void LogBindError(Exception ex, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reverse proxy route {Identifier} started: listening on {Port}, forwarding to {BackendHost}:{BackendPort}")]
    private partial void LogStarted(string identifier, int port, string backendHost, int backendPort);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reverse proxy route {Identifier} stopped")]
    private partial void LogStopped(string identifier);

    private async Task RunAcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var forwardTask = ForwardConnectionAsync(client, cancellationToken);
            lock (_pendingForwards)
            {
                _pendingForwards.Add(forwardTask);
                _pendingForwards.RemoveAll(static task => task.IsCompleted);
            }
        }
    }
}
