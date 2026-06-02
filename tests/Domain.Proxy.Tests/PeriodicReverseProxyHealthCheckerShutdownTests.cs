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
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.HasCancelSucceededAsync" />
    ///     returns <see langword="true" /> when the source is alive.
    /// </summary>
    [Test]
    public async Task HasCancelSucceededAsync_AliveSource_ReturnsTrue()
    {
        var source = new CancellationTokenSource();
        try
        {
            var ok = await PeriodicReverseProxyHealthCheckerShutdown.HasCancelSucceededAsync(source, CancellationToken.None);

            await Assert.That(ok).IsTrue();
            await Assert.That(source.Token.IsCancellationRequested).IsTrue();
        }
        finally
        {
            source.Dispose();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicReverseProxyHealthCheckerShutdown.HasCancelSucceededAsync" />
    ///     returns <see langword="false" /> when the source has already been disposed.
    /// </summary>
    [Test]
    public async Task HasCancelSucceededAsync_DisposedSource_ReturnsFalse()
    {
        var source = new CancellationTokenSource();
        source.Dispose();

        var ok = await PeriodicReverseProxyHealthCheckerShutdown.HasCancelSucceededAsync(source, CancellationToken.None);

        await Assert.That(ok).IsFalse();
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
