using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Helper coroutines that <see cref="PeriodicReverseProxyHealthChecker" /> uses while
///     shutting down. Extracted into a static class so the analyzer's
///     static-in-non-static-class rule (ATXCS011) is satisfied.
/// </summary>
public static class PeriodicReverseProxyHealthCheckerShutdown
{
    /// <summary>
    ///     Cancels the supplied <paramref name="source" /> and returns whether the cancel
    ///     completed normally. Swallows <see cref="ObjectDisposedException" /> so a parallel
    ///     <see cref="IDisposable.Dispose" /> race is tolerated.
    /// </summary>
    /// <param name="source">The cancellation token source to cancel.</param>
    /// <param name="cancellationToken">
    ///     Cancels the wait for the cancel operation itself. When this token fires while
    ///     <see cref="CancellationTokenSource.CancelAsync" /> is draining registered
    ///     callbacks, the resulting <see cref="OperationCanceledException" /> is propagated
    ///     so the caller's timeout is honored.
    /// </param>
    /// <returns><see langword="true" /> when the cancel propagated normally.</returns>
    public static async Task<bool> HasCancelSucceededAsync(
        CancellationTokenSource source,
        CancellationToken cancellationToken)
    {
        try
        {
            await source.CancelAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ObjectDisposedException ex)
        {
            _ = ex;
            return false;
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
    /// <returns>A task that completes when the loop drains.</returns>
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
