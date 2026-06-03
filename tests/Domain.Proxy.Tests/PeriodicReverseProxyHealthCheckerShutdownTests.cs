using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="PeriodicReverseProxyHealthCheckerShutdown" />.
/// </summary>
public sealed class PeriodicReverseProxyHealthCheckerShutdownTests
{
    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.CancelAndDisposeAsync" />
    ///     cancels an alive source and disposes it.
    /// </summary>
    [Test]
    public async Task CancelAndDisposeAsync_AliveSource_CancelsAndDisposes()
    {
        var source = new CancellationTokenSource();
        var token = source.Token;

        await PeriodicReverseProxyHealthCheckerShutdown.CancelAndDisposeAsync(source, CancellationToken.None);

        await Assert.That(token.IsCancellationRequested).IsTrue();
        await Assert.That(() => source.Token).Throws<ObjectDisposedException>();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.CancelAndDisposeAsync" />
    ///     tolerates a source that has already been disposed.
    /// </summary>
    [Test]
    public async Task CancelAndDisposeAsync_DisposedSource_ReturnsWithoutThrowing()
    {
        var source = new CancellationTokenSource();
        source.Dispose();

        await PeriodicReverseProxyHealthCheckerShutdown.CancelAndDisposeAsync(source, CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync" />
    ///     swallows an <see cref="OperationCanceledException" /> thrown by the loop.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_LoopThrowsCancelled_Swallows()
    {
        var loop = Task.FromException(new OperationCanceledException("loop cancelled"));

        await PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync(loop, CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync" />
    ///     propagates an <see cref="OperationCanceledException" /> when the caller's wait
    ///     token fires, so callers can honor their own timeout.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_WaitTokenCancelled_Throws()
    {
        var taskCompletionSource = new TaskCompletionSource();
        using var cts = new CancellationTokenSource();
        var waitTask = PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync(taskCompletionSource.Task, cts.Token);
        await cts.CancelAsync();

        await Assert.That(async () => await waitTask).Throws<OperationCanceledException>();

        taskCompletionSource.SetResult();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync" />
    ///     returns normally when the loop completes successfully.
    /// </summary>
    [Test]
    public async Task WaitForLoopAsync_LoopCompletes_ReturnsNormally()
    {
        await PeriodicReverseProxyHealthCheckerShutdown.WaitForLoopAsync(Task.CompletedTask, CancellationToken.None);
    }
}
