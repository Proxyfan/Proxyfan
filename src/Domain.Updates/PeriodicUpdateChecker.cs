using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Background poller that repeatedly invokes an <see cref="IUpdateChecker" /> and
///     forwards results to a <see cref="MutableUpdateNotification" />. Polls survive
///     transient exceptions; the loop only exits on <see cref="StopAsync" /> or
///     <see cref="Dispose" />.
/// </summary>
public sealed class PeriodicUpdateChecker : IDisposable
{
    private readonly IUpdateChecker _checker;
    private readonly Lock _lock;
    private readonly MutableUpdateNotification _notification;
    private readonly PeriodicUpdateCheckOptions _options;
    private CancellationTokenSource? _cancellationSource;
    private bool _isDisposed;
    private Task? _loop;
    private int _pollFailureCount;

    /// <summary>
    ///     Initializes a new <see cref="PeriodicUpdateChecker" />.
    /// </summary>
    /// <param name="checker">The underlying update checker.</param>
    /// <param name="notification">The observable notification to publish into.</param>
    /// <param name="options">Polling timing options.</param>
    public PeriodicUpdateChecker(
        IUpdateChecker checker,
        MutableUpdateNotification notification,
        PeriodicUpdateCheckOptions options)
    {
        _checker = checker;
        _notification = notification;
        _options = options;
        var newLock = new Lock();
        _lock = newLock;
    }

    /// <summary>
    ///     Disposes the checker, cancelling any in-flight poll.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        var source = _cancellationSource;
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException ex)
        {
            _ = ex;
        }

        source.Dispose();
    }

    /// <summary>
    ///     Starts the background polling loop if not already running.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_loop is not null)
            {
                return;
            }

            var newSource = new CancellationTokenSource();
            _cancellationSource = newSource;
            var newLoop = Task.Run(() => RunAsync(newSource.Token));
            _loop = newLoop;
        }
    }

    /// <summary>
    ///     Stops the background polling loop and waits for it to drain.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the loop to drain.</param>
    /// <returns>A task that completes once the loop has been signalled to stop.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? source;
        Task? loop;
        lock (_lock)
        {
            source = _cancellationSource;
            loop = _loop;
            _cancellationSource = null;
            _loop = null;
        }

        if (source is not null)
        {
            await PeriodicUpdateCheckerShutdown
                .CancelAndDisposeAsync(source, cancellationToken)
                .ConfigureAwait(false);
        }

        if (loop is not null)
        {
            await PeriodicUpdateCheckerShutdown
                .WaitForLoopAsync(loop, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await _checker.CheckAsync(_options.CurrentVersion, cancellationToken).ConfigureAwait(false);
            _notification.Publish(info);
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
            throw;
        }
        catch (Exception ex)
        {
            _ = ex;
            Interlocked.Increment(ref _pollFailureCount);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_options.InitialDelay, cancellationToken).ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                await PollOnceAsync(cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await Task.Delay(_options.PollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }
}
