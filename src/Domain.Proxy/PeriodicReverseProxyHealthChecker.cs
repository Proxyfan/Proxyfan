using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Background poller that periodically probes every non-stopped reverse-proxy route's
///     backend, updating its status so the UI can render up-to-date health information
///     without waiting for an on-demand probe. Survives per-route exceptions; the loop
///     only exits on <see cref="StopAsync" /> or <see cref="Dispose" />.
/// </summary>
public sealed class PeriodicReverseProxyHealthChecker : IDisposable
{
    private readonly IReverseProxyEngine _engine;
    private readonly Lock _lock;
    private readonly PeriodicReverseProxyHealthCheckOptions _options;
    private CancellationTokenSource? _cancellationSource;
    private bool _isDisposed;
    private Task? _loop;

    /// <summary>
    ///     Initializes a new <see cref="PeriodicReverseProxyHealthChecker" />.
    /// </summary>
    /// <param name="engine">The engine whose routes will be probed.</param>
    /// <param name="options">Polling timing options.</param>
    public PeriodicReverseProxyHealthChecker(
        IReverseProxyEngine engine,
        PeriodicReverseProxyHealthCheckOptions options)
    {
        if (options.InitialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.InitialDelay,
                $"{nameof(PeriodicReverseProxyHealthCheckOptions.InitialDelay)} must be greater than or equal to TimeSpan.Zero.");
        }

        if (options.PollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.PollInterval,
                $"{nameof(PeriodicReverseProxyHealthCheckOptions.PollInterval)} must be greater than TimeSpan.Zero.");
        }

        _engine = engine;
        _options = options;
        var newLock = new Lock();
        _lock = newLock;
    }

    /// <summary>
    ///     Disposes the checker, cancelling any in-flight probe.
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
            await PeriodicReverseProxyHealthCheckerShutdown
                .CancelAndDisposeAsync(source, cancellationToken)
                .ConfigureAwait(false);
        }

        if (loop is not null)
        {
            await PeriodicReverseProxyHealthCheckerShutdown
                .WaitForLoopAsync(loop, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ProbeAllOnceAsync(CancellationToken cancellationToken)
    {
        var states = _engine.GetStates();
        foreach (var state in states)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (state.Status == ReverseProxyRouteStatus.Stopped)
            {
                continue;
            }

            await ProbeSingleAsync(state.Route.Identifier, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProbeSingleAsync(string identifier, CancellationToken cancellationToken)
    {
        try
        {
            await _engine.ProbeAsync(identifier, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
            throw;
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_options.InitialDelay, cancellationToken).ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProbeAllOnceAsync(cancellationToken).ConfigureAwait(false);
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
