using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for <see cref="PeriodicUpdateCheckerShutdown" />.
/// </summary>
public sealed class PeriodicUpdateCheckerShutdownTests
{
    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateCheckerShutdown.HasCancelSucceededAsync" />
    ///     returns <see langword="true" /> when the source is alive.
    /// </summary>
    [Test]
    public async Task HasCancelSucceededAsync_AliveSource_ReturnsTrue()
    {
        var source = new CancellationTokenSource();
        try
        {
            var ok = await PeriodicUpdateCheckerShutdown.HasCancelSucceededAsync(source, CancellationToken.None);

            await Assert.That(ok).IsTrue();
            await Assert.That(source.Token.IsCancellationRequested).IsTrue();
        }
        finally
        {
            source.Dispose();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateCheckerShutdown.HasCancelSucceededAsync" />
    ///     returns <see langword="false" /> when the source has already been disposed.
    /// </summary>
    [Test]
    public async Task HasCancelSucceededAsync_DisposedSource_ReturnsFalse()
    {
        var source = new CancellationTokenSource();
        source.Dispose();

        var ok = await PeriodicUpdateCheckerShutdown.HasCancelSucceededAsync(source, CancellationToken.None);

        await Assert.That(ok).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateCheckerShutdown.WaitForLoopAsync" /> swallows
    ///     an <see cref="OperationCanceledException" /> thrown by the loop.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_LoopThrowsCancelled_Swallows()
    {
        var loop = Task.FromException(new OperationCanceledException("loop cancelled"));

        await PeriodicUpdateCheckerShutdown.WaitForLoopAsync(loop, CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateCheckerShutdown.WaitForLoopAsync" /> swallows
    ///     a cancellation token that fires while the wait is in flight.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_WaitTokenCancelled_Swallows()
    {
        var taskCompletionSource = new TaskCompletionSource();
        using var cts = new CancellationTokenSource();
        var waitTask = PeriodicUpdateCheckerShutdown.WaitForLoopAsync(taskCompletionSource.Task, cts.Token);
        await cts.CancelAsync();
        await waitTask;
        taskCompletionSource.SetResult();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateCheckerShutdown.WaitForLoopAsync" /> returns
    ///     normally when the loop completes successfully.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_LoopCompletes_ReturnsNormally()
    {
        await PeriodicUpdateCheckerShutdown.WaitForLoopAsync(Task.CompletedTask, CancellationToken.None);
    }
}
