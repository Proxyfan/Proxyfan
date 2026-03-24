using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy.Events;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Aggregate root that manages the complete proxy server lifecycle: configuration,
///     start, stop, restart, and status reporting.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="ProxyServer" /> coordinates the <see cref="IProxyListener" /> abstraction
///         and publishes domain events for lifecycle changes via <see cref="IDomainEventBus" />.
///         Both the UI and CLI interact exclusively with this class to control the proxy.
///     </para>
///     <para>
///         If <see cref="ProxyOptions.AutoStart" /> is <see langword="true" /> when the server is
///         constructed, <see cref="StartAsync(CancellationToken)" /> is fired asynchronously in
///         the background. Errors during auto-start transition the server to
///         <see cref="ProxyStatus.Faulted" />.
///     </para>
///     <para>
///         Configuration changes detected via <see cref="IOptionsMonitor{TOptions}" /> automatically
///         trigger a restart when the server is running. Dispose stops the server and releases all
///         resources.
///     </para>
/// </remarks>
public sealed partial class ProxyServer : IAsyncDisposable
{
    private readonly IProxyListener _listener;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private readonly IDomainEventBus _eventBus;
    private readonly ILogger<ProxyServer> _logger;

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly IDisposable? _optionsChangeSubscription;

    private volatile ProxyStatus _status = ProxyStatus.Stopped;
    private CancellationTokenSource? _listenerCts;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new <see cref="ProxyServer" /> and, if
    ///     <see cref="ProxyOptions.AutoStart" /> is enabled, begins listening asynchronously.
    /// </summary>
    /// <param name="listener">The TCP proxy listener to delegate to.</param>
    /// <param name="optionsMonitor">Live options monitor for <see cref="ProxyOptions" />.</param>
    /// <param name="eventBus">Domain event bus for publishing lifecycle events.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    public ProxyServer(
        IProxyListener listener,
        IOptionsMonitor<ProxyOptions> optionsMonitor,
        IDomainEventBus eventBus,
        ILogger<ProxyServer> logger)
    {
        _listener = listener;
        _optionsMonitor = optionsMonitor;
        _eventBus = eventBus;
        _logger = logger;

        _optionsChangeSubscription = optionsMonitor.OnChange(OnOptionsChanged);

        if (optionsMonitor.CurrentValue.AutoStart)
        {
            _ = Task.Run(() => StartAsync(CancellationToken.None));
        }
    }

    /// <summary>Gets the current lifecycle status of the proxy server.</summary>
    public ProxyStatus Status => _status;

    /// <summary>
    ///     Gets the port number the listener is currently bound to, or <see langword="null" />
    ///     when the server is not running.
    /// </summary>
    public int? BoundPort => _listener.BoundPort;

    /// <summary>
    ///     Starts the proxy server. If the server is already <see cref="ProxyStatus.Running" />
    ///     or <see cref="ProxyStatus.Starting" />, the call is a no-op and returns success.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the start operation.</param>
    /// <returns>
    ///     <see cref="Result.Success()" /> when the server is listening;
    ///     <see cref="Result.Failure(DomainError)" /> with a <see cref="ProxyBindError" /> or
    ///     <see cref="ProxyFaultedError" /> on failure.
    /// </returns>
    public async Task<Result> StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await StartCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    ///     Stops the proxy server gracefully. If the server is already
    ///     <see cref="ProxyStatus.Stopped" /> or <see cref="ProxyStatus.Stopping" />, the
    ///     call is a no-op and returns success.
    /// </summary>
    /// <param name="cancellationToken">A token that forces an immediate stop if cancelled.</param>
    /// <returns>
    ///     <see cref="Result.Success()" /> when the server has stopped;
    ///     <see cref="Result.Failure(DomainError)" /> with a <see cref="ProxyFaultedError" />
    ///     on unexpected failure.
    /// </returns>
    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await StopCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    ///     Restarts the proxy server atomically under a single lifecycle lock. If the server
    ///     is not running, this is equivalent to <see cref="StartAsync(CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the restart operation.</param>
    /// <returns>
    ///     <see cref="Result.Success()" /> when the server is listening again;
    ///     <see cref="Result.Failure(DomainError)" /> on failure.
    /// </returns>
    public async Task<Result> RestartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_status is ProxyStatus.Running or ProxyStatus.Faulted)
            {
                var stopResult = await StopCoreAsync(cancellationToken).ConfigureAwait(false);

                if (!stopResult.IsSuccess)
                {
                    return stopResult;
                }
            }

            return await StartCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _optionsChangeSubscription?.Dispose();

        await _lifecycleLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_status is ProxyStatus.Running or ProxyStatus.Starting or ProxyStatus.Faulted)
            {
                await StopCoreAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally
        {
            _lifecycleLock.Release();
            _lifecycleLock.Dispose();
        }
    }

    // ── Core helpers (called with lifecycle lock held) ──────────────────────

    private async Task<Result> StartCoreAsync(CancellationToken cancellationToken)
    {
        if (_status is ProxyStatus.Running or ProxyStatus.Starting)
        {
            LogStartNoOp(_status);
            return Result.Success();
        }

        var options = _optionsMonitor.CurrentValue;
        LogStarting(options.Port);

        SetStatus(ProxyStatus.Starting);

        try
        {
            _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _listener.StartAsync(OnConnectionAccepted, _listenerCts.Token).ConfigureAwait(false);

            SetStatus(ProxyStatus.Running);

            var boundPort = _listener.BoundPort ?? options.Port;
            LogStarted(boundPort);
            _eventBus.Publish(new ProxyStarted(boundPort, DateTimeOffset.UtcNow));

            return Result.Success();
        }
        catch (ProxyBindException ex)
        {
            SetStatus(ProxyStatus.Faulted);

            var error = new ProxyBindError(options.Port, ex);
            LogBindFailed(ex, options.Port);
            _eventBus.Publish(new ProxyErrorOccurred(error, DateTimeOffset.UtcNow));

            return Result.Failure(error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ProxyStatus.Faulted);

            var error = new ProxyFaultedError("Start", ex);
            LogUnexpectedStartError(ex);
            _eventBus.Publish(new ProxyErrorOccurred(error, DateTimeOffset.UtcNow));

            return Result.Failure(error);
        }
    }

    private async Task<Result> StopCoreAsync(CancellationToken cancellationToken)
    {
        if (_status is ProxyStatus.Stopped or ProxyStatus.Stopping)
        {
            LogStopNoOp(_status);
            return Result.Success();
        }

        LogStopping();

        SetStatus(ProxyStatus.Stopping);

        try
        {
            if (_listenerCts is not null)
            {
                await _listenerCts.CancelAsync().ConfigureAwait(false);
            }

            await _listener.StopAsync(cancellationToken).ConfigureAwait(false);

            SetStatus(ProxyStatus.Stopped);

            _listenerCts?.Dispose();
            _listenerCts = null;

            LogStopped();
            _eventBus.Publish(new ProxyStopped(DateTimeOffset.UtcNow));

            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ProxyStatus.Faulted);

            var error = new ProxyFaultedError("Stop", ex);
            LogUnexpectedStopError(ex);
            _eventBus.Publish(new ProxyErrorOccurred(error, DateTimeOffset.UtcNow));

            return Result.Failure(error);
        }
    }

    private void SetStatus(ProxyStatus status)
    {
        _status = status;
    }

    private static Task OnConnectionAccepted(IProxyConnection connection, CancellationToken cancellationToken)
    {
        // Connection dispatching is handled by downstream tasks (T02 ConnectionDispatcher).
        // Dispose the connection so the socket is released.
        return connection.DisposeAsync().AsTask();
    }

    private void OnOptionsChanged(ProxyOptions options, string? name)
    {
        LogConfigurationChanged();
        _ = Task.Run(() => RestartAsync(CancellationToken.None));
    }

    // ── Logger messages ──────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Debug, Message = "StartAsync called while proxy is already {Status}; ignoring.")]
    private partial void LogStartNoOp(ProxyStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting proxy server on port {Port}.")]
    private partial void LogStarting(int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy server started on port {Port}.")]
    private partial void LogStarted(int port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server failed to bind to port {Port}.")]
    private partial void LogBindFailed(Exception exception, int port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server encountered an unexpected error during start.")]
    private partial void LogUnexpectedStartError(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "StopAsync called while proxy is already {Status}; ignoring.")]
    private partial void LogStopNoOp(ProxyStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping proxy server.")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy server stopped.")]
    private partial void LogStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server encountered an unexpected error during stop.")]
    private partial void LogUnexpectedStopError(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy configuration changed. Scheduling restart.")]
    private partial void LogConfigurationChanged();
}
