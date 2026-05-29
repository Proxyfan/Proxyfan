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
    ///     Cancels the supplied <paramref name="source" /> and returns whether the cancel
    ///     completed normally. Swallows <see cref="ObjectDisposedException" /> so a parallel
    ///     <see cref="IDisposable.Dispose" /> race is tolerated.
    /// </summary>
    /// <param name="source">The cancellation token source to cancel.</param>
    /// <param name="cancellationToken">Cancels the cancel operation itself.</param>
    /// <returns><see langword="true" /> when the cancel propagated normally.</returns>
    public static async Task<bool> HasCancelSucceededAsync(
        CancellationTokenSource source,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        try
        {
            await source.CancelAsync().ConfigureAwait(false);
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
    ///     swallowing the <see cref="OperationCanceledException" /> that propagates when
    ///     <see cref="Task.Delay(TimeSpan, CancellationToken)" /> aborts.
    /// </summary>
    /// <param name="loop">The background loop task to wait for.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task that completes when the loop drains or the wait is cancelled.</returns>
    public static async Task WaitForLoopAsync(Task loop, CancellationToken cancellationToken)
    {
        try
        {
            await loop.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
        }
    }
}
