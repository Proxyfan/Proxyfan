using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for the lifecycle edge cases on <see cref="PeriodicUpdateChecker" /> not covered
///     by the primary <see cref="PeriodicUpdateCheckerTests" /> suite: Dispose with an active
///     loop, Start after Dispose, idempotent Dispose, and concurrent cancel/dispose races.
/// </summary>
public sealed class PeriodicUpdateCheckerLifecycleTests
{
    private const int WaitTimeoutMilliseconds = 2000;

    /// <summary>
    ///     Verifies that calling <see cref="PeriodicUpdateChecker.Dispose" /> while the loop
    ///     is running cancels the loop's cancellation token so the in-flight poll observes it.
    /// </summary>
    [Test]
    public async Task Dispose_WithActiveLoop_CancelsInflightWork()
    {
        var checker = new BlockingUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(20),
        };
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForCheckStartedAsync();
        periodic.Dispose();

        await Assert.That(await checker.WaitForCancellationAsync()).IsTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateChecker.Start" /> after Dispose is a no-op:
    ///     it does not spin a new loop, so no poll is observed.
    /// </summary>
    [Test]
    public async Task Start_AfterDispose_DoesNotInvokeChecker()
    {
        var checker = new BlockingUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(20),
        };
        var periodic = new PeriodicUpdateChecker(checker, notification, options);
        periodic.Dispose();

        periodic.Start();
        var checkStarted = await checker.WaitForCheckStartedWithinAsync(TimeSpan.FromMilliseconds(200));

        await Assert.That(checkStarted).IsFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="PeriodicUpdateChecker.StopAsync" /> awaits the in-flight
    ///     poll before disposing the cancellation source, so the loop never observes an
    ///     <see cref="ObjectDisposedException" /> when interacting with the token.
    /// </summary>
    [Test]
    public async Task StopAsync_WithInflightCheck_DoesNotDisposeSourceBeforeLoopDrains()
    {
        var checker = new TokenRegisteringUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(20),
        };
        using var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForCheckStartedAsync();

        var stopTask = periodic.StopAsync(CancellationToken.None);
        using var settleDelay = new SemaphoreSlim(0, 1);
        _ = await settleDelay.WaitAsync(TimeSpan.FromMilliseconds(50));
        var stopCompletedBeforeRelease = stopTask.IsCompleted;
        checker.ReleaseGate();
        await stopTask;

        await Assert.That(stopCompletedBeforeRelease).IsFalse();
        await Assert.That(checker.RegistrationException).IsNull();
    }

    /// <summary>
    ///     Verifies that double-disposing the checker is a safe no-op.
    /// </summary>
    [Test]
    public async Task Dispose_CalledTwice_IsIdempotent()
    {
        var checker = new BlockingUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(20),
        };
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Dispose();
        periodic.Dispose();

        await Assert.That(checker.CheckStartedCount).IsEqualTo(0);
    }

    private sealed class BlockingUpdateChecker : IUpdateChecker, IDisposable
    {
        private readonly SemaphoreSlim _checkStarted;
        private readonly TaskCompletionSource<bool> _cancellationObserved;
        private int _checkStartedCount;

        public BlockingUpdateChecker()
        {
            _checkStarted = new SemaphoreSlim(0);
            _cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public int CheckStartedCount => Volatile.Read(ref _checkStartedCount);

        public async Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _checkStartedCount);
            _checkStarted.Release();
            try
            {
                using var blocker = new SemaphoreSlim(0, 1);
                await blocker.WaitAsync(cancellationToken);
                return null;
            }
            catch (OperationCanceledException)
            {
                _cancellationObserved.TrySetResult(true);
                throw;
            }
        }

        public async Task WaitForCheckStartedAsync()
        {
            await _checkStarted.WaitAsync(WaitTimeoutMilliseconds);
        }

        public async Task<bool> WaitForCheckStartedWithinAsync(TimeSpan timeout)
        {
            return await _checkStarted.WaitAsync(timeout);
        }

        public async Task<bool> WaitForCancellationAsync()
        {
            var timeoutSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var ctsForTimeout = new CancellationTokenSource(WaitTimeoutMilliseconds);
            using var registration = ctsForTimeout.Token.Register(() => timeoutSource.TrySetResult(false));
            var completed = await Task.WhenAny(_cancellationObserved.Task, timeoutSource.Task);
            return completed == _cancellationObserved.Task;
        }

        public void Dispose()
        {
            _checkStarted.Dispose();
        }
    }

    private sealed class TokenRegisteringUpdateChecker : IUpdateChecker, IDisposable
    {
        private readonly SemaphoreSlim _checkStarted;
        private readonly TaskCompletionSource _gate;

        public TokenRegisteringUpdateChecker()
        {
            _checkStarted = new SemaphoreSlim(0);
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Exception? RegistrationException { get; private set; }

        public async Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken)
        {
            _ = currentVersion;
            _checkStarted.Release();
            await _gate.Task.ConfigureAwait(false);
            try
            {
                var handle = cancellationToken.WaitHandle;
                _ = handle.WaitOne(0);
            }
            catch (ObjectDisposedException ex)
            {
                RegistrationException = ex;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        public async Task WaitForCheckStartedAsync()
        {
            await _checkStarted.WaitAsync(WaitTimeoutMilliseconds);
        }

        public void ReleaseGate()
        {
            _gate.TrySetResult();
        }

        public void Dispose()
        {
            _checkStarted.Dispose();
        }
    }
}

