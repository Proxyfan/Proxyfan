using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxyfan.Domain.Proxy.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Aggregate root that manages the complete proxy server lifecycle: configuration,
///     start, stop, restart, and status reporting.
///     <see cref="ProxyServer" /> coordinates the <see cref="IProxyListener" /> abstraction
///     and publishes domain events for lifecycle changes via <see cref="IDomainEventBus" />.
///     Both the UI and CLI interact exclusively with this class to control the proxy.
///     If <see cref="ProxyOptions.IsAutoStart" /> is <see langword="true" /> when the server is
///     constructed, <see cref="StartAsync(CancellationToken)" /> is fired asynchronously in
///     the background. Errors during auto-start transition the server to
///     <see cref="ProxyStatus.Faulted" />.
///     Configuration changes detected via <see cref="IOptionsMonitor{TOptions}" /> automatically
///     trigger a restart when the server is running. Dispose stops the server and releases all
///     resources.
/// </summary>
public sealed partial class ProxyServer : IAsyncDisposable
{
    private readonly Task<VoidResult>? _autoStartTask;
    private readonly IConnectionDispatcher _dispatcher;
    private readonly IDomainEventBus _eventBus;
    private readonly SemaphoreSlim _lifecycleLock;
    private readonly IProxyListener _listener;
    private readonly ILogger<ProxyServer> _logger;
    private readonly IDisposable? _optionsChangeSubscription;
    private readonly IOptionsMonitor<ProxyOptions> _optionsMonitor;
    private bool _isDisposed;
    private CancellationTokenSource? _listenerCancellationSource;
    private Task<VoidResult>? _restartTask;
    private volatile ProxyStatus _status;

    /// <summary>
    ///     Gets the port number the listener is currently bound to, or <see langword="null" />
    ///     when the server is not running.
    /// </summary>
    public int? BoundPort => _listener.BoundPort;

    /// <summary>
    ///     Gets the current lifecycle status of the proxy server.
    /// </summary>
    public ProxyStatus Status => _status;

    /// <summary>
    ///     Initializes a new <see cref="ProxyServer" /> and, if
    ///     <see cref="ProxyOptions.IsAutoStart" /> is enabled, begins listening asynchronously.
    /// </summary>
    /// <param name="listener">The TCP proxy listener to delegate to.</param>
    /// <param name="dispatcher">The connection dispatcher that handles accepted connections.</param>
    /// <param name="optionsMonitor">Live options monitor for <see cref="ProxyOptions" />.</param>
    /// <param name="eventBus">Domain event bus for publishing lifecycle events.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    public ProxyServer(
        IProxyListener listener,
        IConnectionDispatcher dispatcher,
        IOptionsMonitor<ProxyOptions> optionsMonitor,
        IDomainEventBus eventBus,
        ILogger<ProxyServer> logger)
    {
        _listener = listener;
        _dispatcher = dispatcher;
        _optionsMonitor = optionsMonitor;
        _eventBus = eventBus;
        _logger = logger;

        var lifecycleLock = new SemaphoreSlim(1, 1);
        _lifecycleLock = lifecycleLock;
        _status = ProxyStatus.Stopped;

        _optionsChangeSubscription = optionsMonitor.OnChange(OnOptionsChanged);

        if (optionsMonitor.CurrentValue.IsAutoStart)
        {
            var autoStartTask = StartAsync(CancellationToken.None);
            _autoStartTask = autoStartTask;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _optionsChangeSubscription?.Dispose();

        await ObserveBackgroundTaskAsync(_autoStartTask, CancellationToken.None).ConfigureAwait(false);
        await ObserveBackgroundTaskAsync(_restartTask, CancellationToken.None).ConfigureAwait(false);

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

    /// <summary>
    ///     Restarts the proxy server atomically under a single lifecycle lock. If the server
    ///     is not running, this is equivalent to <see cref="StartAsync(CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the restart operation.</param>
    /// <returns>
    ///     <see cref="Result.Success()" /> when the server is listening again;
    ///     <see cref="Result.Failure(DomainError)" /> on failure.
    /// </returns>
    public async Task<VoidResult> RestartAsync(CancellationToken cancellationToken)
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
    public async Task<VoidResult> StartAsync(CancellationToken cancellationToken)
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
    public async Task<VoidResult> StopAsync(CancellationToken cancellationToken)
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server failed to bind to port {Port}.")]
    private partial void LogBindFailed(Exception exception, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy server started on port {Port}.")]
    private partial void LogStarted(int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting proxy server on port {Port}.")]
    private partial void LogStarting(int port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "StartAsync called while proxy is already {Status}; ignoring.")]
    private partial void LogStartNoOp(ProxyStatus status);

    [LoggerMessage(Level = LogLevel.Debug, Message = "StopAsync called while proxy is already {Status}; ignoring.")]
    private partial void LogStopNoOp(ProxyStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy server stopped.")]
    private partial void LogStopped();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping proxy server.")]
    private partial void LogStopping();

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server encountered an unexpected error during start.")]
    private partial void LogUnexpectedStartError(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Proxy server encountered an unexpected error during stop.")]
    private partial void LogUnexpectedStopError(Exception exception);

    private async Task ObserveBackgroundTaskAsync(Task<VoidResult>? task, CancellationToken cancellationToken)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
        }
    }

    private Task OnConnectionAcceptedAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        return _dispatcher.DispatchAsync(connection, cancellationToken);
    }

    private void OnOptionsChanged(ProxyOptions options, string? name)
    {
        _ = options;
        _ = name;
        _logger.LogInformation("Proxy configuration changed. Scheduling restart.");
        var restartTask = RestartAsync(CancellationToken.None);
        _restartTask = restartTask;
    }

    private void SetStatus(ProxyStatus status)
    {
        _status = status;
    }

    private async Task<VoidResult> StartCoreAsync(CancellationToken cancellationToken)
    {
        if (_status is ProxyStatus.Running or ProxyStatus.Starting)
        {
            LogStartNoOp(_status);
            return Result.Success();
        }

        var previousStatus = _status;
        var options = _optionsMonitor.CurrentValue;
        LogStarting(options.Port);

        SetStatus(ProxyStatus.Starting);
        CancellationTokenSource? linkedTokenSource = null;

        try
        {
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenerCancellationSource = linkedTokenSource;

            await _listener.StartAsync(OnConnectionAcceptedAsync, _listenerCancellationSource.Token).ConfigureAwait(false);
            return StartSucceeded(options);
        }
        catch (OperationCanceledException)
        {
            SetStatus(previousStatus);
            TryDisposeListenerCancellationSource(linkedTokenSource);
            throw;
        }
        catch (ProxyBindException ex)
        {
            SetStatus(ProxyStatus.Faulted);
            TryDisposeListenerCancellationSource(linkedTokenSource);

            var error = new ProxyBindError(options.Port, ex);
            LogBindFailed(ex, options.Port);

            var errorEvent = new ProxyErrorOccurred(error, DateTimeOffset.UtcNow);
            _eventBus.Publish(errorEvent);

            return Result.Failure(error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ProxyStatus.Faulted);
            TryDisposeListenerCancellationSource(linkedTokenSource);

            var error = new ProxyFaultedError("Start", ex);
            LogUnexpectedStartError(ex);

            var errorEvent = new ProxyErrorOccurred(error, DateTimeOffset.UtcNow);
            _eventBus.Publish(errorEvent);

            return Result.Failure(error);
        }
    }

    private VoidResult StartSucceeded(ProxyOptions options)
    {
        SetStatus(ProxyStatus.Running);
        var boundPort = _listener.BoundPort ?? options.Port;
        LogStarted(boundPort);
        var startedEvent = new ProxyStarted(boundPort, DateTimeOffset.UtcNow);
        _eventBus.Publish(startedEvent);
        return Result.Success();
    }

    private async Task<VoidResult> StopCoreAsync(CancellationToken cancellationToken)
    {
        if (_status is ProxyStatus.Stopped or ProxyStatus.Stopping)
        {
            LogStopNoOp(_status);
            return Result.Success();
        }

        var previousStatus = _status;
        LogStopping();

        SetStatus(ProxyStatus.Stopping);

        try
        {
            if (_listenerCancellationSource is not null)
            {
                await _listenerCancellationSource.CancelAsync().ConfigureAwait(false);
            }

            await _listener.StopAsync(cancellationToken).ConfigureAwait(false);

            SetStatus(ProxyStatus.Stopped);

            TryDisposeListenerCancellationSource(_listenerCancellationSource);

            LogStopped();

            var stoppedEvent = new ProxyStopped(DateTimeOffset.UtcNow);
            _eventBus.Publish(stoppedEvent);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            SetStatus(previousStatus);
            TryDisposeListenerCancellationSource(_listenerCancellationSource);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(ProxyStatus.Faulted);
            TryDisposeListenerCancellationSource(_listenerCancellationSource);

            var error = new ProxyFaultedError("Stop", ex);
            LogUnexpectedStopError(ex);

            var errorEvent = new ProxyErrorOccurred(error, DateTimeOffset.UtcNow);
            _eventBus.Publish(errorEvent);

            return Result.Failure(error);
        }
    }

    private void TryDisposeListenerCancellationSource(CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
        {
            return;
        }

        if (ReferenceEquals(_listenerCancellationSource, cancellationTokenSource))
        {
            _listenerCancellationSource = null;
        }

        cancellationTokenSource.Dispose();
    }
}
