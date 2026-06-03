using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates;

/// <summary>
///     Helper coroutines that <see cref="PeriodicUpdateChecker" /> uses while shutting
///     down. Extracted into a static class so the analyzer's static-in-non-static-class
///     rule (ATXCS011) is satisfied.
/// </summary>
public static class PeriodicUpdateCheckerShutdown
{
    /// <summary>
    ///     Cancels the supplied <paramref name="source" /> and disposes it once the cancel
    ///     drains. When the caller's <paramref name="cancellationToken" /> fires before the
    ///     cancel completes, the resulting <see cref="OperationCanceledException" /> is
    ///     propagated and <see cref="IDisposable.Dispose" /> is NOT invoked: disposing while
    ///     <see cref="CancellationTokenSource.CancelAsync" /> is still draining callbacks
    ///     is not thread-safe, so the source is left for finalization rather than racing
    ///     the in-flight cancel. <see cref="ObjectDisposedException" /> from a parallel
    ///     dispose race is swallowed.
    /// </summary>
    /// <param name="source">The cancellation token source to cancel and dispose.</param>
    /// <param name="cancellationToken">Cancels the wait for the cancel operation.</param>
    /// <returns>
    ///     A task that completes successfully when the cancel drained and the source was
    ///     disposed. May complete as cancelled when <paramref name="cancellationToken" />
    ///     fires before the cancel drains; in that case the source is intentionally left
    ///     undisposed rather than risking a concurrent dispose / cancel race.
    /// </returns>
    public static async Task CancelAndDisposeAsync(
        CancellationTokenSource source,
        CancellationToken cancellationToken)
    {
        Task cancelTask;
        try
        {
            cancelTask = source.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            await cancelTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            source.Dispose();
        }
        catch (ObjectDisposedException)
        {
            _ = source;
        }
    }

    /// <summary>
    ///     Waits for the supplied background <paramref name="loop" /> task to complete,
    ///     swallowing the <see cref="OperationCanceledException" /> that propagates from
    ///     the loop's own cancellation (for example when
    ///     <see cref="Task.Delay(TimeSpan, CancellationToken)" /> aborts inside the loop).
    ///     A cancellation triggered by the caller's <paramref name="cancellationToken" /> is
    ///     re-thrown so callers can honor their own timeout instead of silently assuming the
    ///     loop drained.
    /// </summary>
    /// <param name="loop">The background loop task to wait for.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>
    ///     A task that completes when the loop drains. May complete as cancelled when
    ///     <paramref name="cancellationToken" /> fires before the loop drains; in that case
    ///     the loop is left running to drain on its own.
    /// </returns>
    public static async Task WaitForLoopAsync(Task loop, CancellationToken cancellationToken)
    {
        try
        {
            await loop.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }
}
