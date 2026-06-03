using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Updates;

namespace Proxyfan.Domain.Updates.Tests;

/// <summary>
///     Tests for <see cref="PeriodicUpdateChecker" />. Uses short real-time intervals
///     (~30 ms) plus signal-driven waits to exercise the polling loop deterministically.
/// </summary>
public sealed class PeriodicUpdateCheckerTests
{
    private const int PollIntervalMilliseconds = 30;
    private const int WaitTimeoutMilliseconds = 2000;

    /// <summary>
    ///     Verifies the first poll publishes the result returned by the checker.
    /// </summary>
    [Test]
    public async Task Start_FirstPoll_PublishesResult()
    {
        var checker = new StubUpdateChecker();
        checker.Enqueue(CreateUpdate("2.0.0"));
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForChecksAsync(expected: 1);
        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(notification.Latest).IsNotNull();
        await Assert.That(notification.Latest!.Version).IsEqualTo("2.0.0");
    }

    /// <summary>
    ///     Verifies the loop polls again after the configured interval elapses.
    /// </summary>
    [Test]
    public async Task Start_AfterInterval_PollsAgain()
    {
        var checker = new StubUpdateChecker();
        checker.Enqueue(CreateUpdate("2.0.0"));
        checker.Enqueue(CreateUpdate("2.1.0"));
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForChecksAsync(expected: 2);
        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(notification.Latest!.Version).IsEqualTo("2.1.0");
    }

    /// <summary>
    ///     Verifies the loop honours the initial delay before the first poll.
    /// </summary>
    [Test]
    public async Task Start_WithInitialDelay_WaitsBeforeFirstPoll()
    {
        var checker = new StubUpdateChecker();
        checker.Enqueue(CreateUpdate("2.0.0"));
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.FromMilliseconds(150),
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        var beforeDelayObserved = await checker.WasCheckObservedWithinAsync(
            TimeSpan.FromMilliseconds(50));
        await checker.WaitForChecksAsync(expected: 1);
        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(beforeDelayObserved).IsFalse();
    }

    /// <summary>
    ///     Verifies StopAsync cancels the loop so no further polls occur.
    /// </summary>
    [Test]
    public async Task StopAsync_DuringLoop_StopsFurtherPolls()
    {
        var checker = new StubUpdateChecker();
        checker.Enqueue(CreateUpdate("2.0.0"));
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForChecksAsync(expected: 1);
        await periodic.StopAsync(CancellationToken.None);
        var checksAfterStop = checker.CheckCount;
        var furtherPolled = await checker.WasCheckObservedWithinAsync(
            TimeSpan.FromMilliseconds(150));

        await Assert.That(furtherPolled).IsFalse();
        await Assert.That(checker.CheckCount).IsEqualTo(checksAfterStop);
    }

    /// <summary>
    ///     Verifies an exception thrown by the checker does not crash the loop and the next
    ///     interval triggers a fresh poll.
    /// </summary>
    [Test]
    public async Task Start_WhenCheckerThrows_LoopContinues()
    {
        var checker = new StubUpdateChecker();
        checker.EnqueueException(new InvalidOperationException("simulated"));
        checker.Enqueue(CreateUpdate("2.0.0"));
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        await checker.WaitForChecksAsync(expected: 2);
        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(notification.Latest).IsNotNull();
        await Assert.That(notification.Latest!.Version).IsEqualTo("2.0.0");
    }

    /// <summary>
    ///     Verifies Start is idempotent when called a second time without stopping.
    /// </summary>
    [Test]
    public async Task Start_CalledTwice_DoesNotDoubleSpinLoop()
    {
        var checker = new StubUpdateChecker();
        checker.Enqueue(CreateUpdate("2.0.0"));
        checker.Enqueue(CreateUpdate("2.0.0"));
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        periodic.Start();
        periodic.Start();
        await checker.WaitForChecksAsync(expected: 2);
        var checksFromFirstWindow = checker.CheckCount;
        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(checksFromFirstWindow).IsLessThanOrEqualTo(4);
    }

    /// <summary>
    ///     Verifies disposing the checker without starting is a no-op.
    /// </summary>
    [Test]
    public async Task Dispose_WithoutStart_DoesNothing()
    {
        var checker = new StubUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        using var periodic = new PeriodicUpdateChecker(checker, notification, options);

        await Assert.That(checker.CheckCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies StopAsync without a prior Start is a safe no-op.
    /// </summary>
    [Test]
    public async Task StopAsync_WithoutStart_IsNoOp()
    {
        var checker = new StubUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = CreateOptions();
        var periodic = new PeriodicUpdateChecker(checker, notification, options);

        await periodic.StopAsync(CancellationToken.None);

        await Assert.That(checker.CheckCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies the constructor rejects a negative <see cref="PeriodicUpdateCheckOptions.InitialDelay" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativeInitialDelay_ThrowsArgumentOutOfRange()
    {
        var checker = new StubUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.FromMilliseconds(-1),
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };

        await Assert
            .That(() => new PeriodicUpdateChecker(checker, notification, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects a zero <see cref="PeriodicUpdateCheckOptions.PollInterval" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroPollInterval_ThrowsArgumentOutOfRange()
    {
        var checker = new StubUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.Zero,
        };

        await Assert
            .That(() => new PeriodicUpdateChecker(checker, notification, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects a negative <see cref="PeriodicUpdateCheckOptions.PollInterval" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativePollInterval_ThrowsArgumentOutOfRange()
    {
        var checker = new StubUpdateChecker();
        var notification = new MutableUpdateNotification();
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(-1),
        };

        await Assert
            .That(() => new PeriodicUpdateChecker(checker, notification, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static PeriodicUpdateCheckOptions CreateOptions()
    {
        var options = new PeriodicUpdateCheckOptions
        {
            CurrentVersion = "1.0.0",
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };
        return options;
    }

    private static UpdateInfo CreateUpdate(string version)
    {
        var info = new UpdateInfo
        {
            Version = version,
            DownloadUrl = "https://example.com/release",
            ReleaseNotes = "notes",
        };
        return info;
    }

    private sealed class StubUpdateChecker : IUpdateChecker
    {
        private readonly Lock _lock;
        private readonly Queue<object?> _results;
        private readonly SemaphoreSlim _checkSignal;
        private int _checkCount;

        public StubUpdateChecker()
        {
            var newQueue = new Queue<object?>();
            var newLock = new Lock();
            var newSignal = new SemaphoreSlim(0);
            _results = newQueue;
            _lock = newLock;
            _checkSignal = newSignal;
        }

        public int CheckCount
        {
            get
            {
                lock (_lock)
                {
                    return _checkCount;
                }
            }
        }

        public Task<UpdateInfo?> CheckAsync(string currentVersion, CancellationToken cancellationToken)
        {
            object? next;
            lock (_lock)
            {
                _checkCount++;
                next = _results.Count > 0 ? _results.Dequeue() : null;
            }

            _checkSignal.Release();
            if (next is Exception exception)
            {
                return Task.FromException<UpdateInfo?>(exception);
            }

            return Task.FromResult((UpdateInfo?)next);
        }

        public void Enqueue(UpdateInfo update)
        {
            lock (_lock)
            {
                _results.Enqueue(update);
            }
        }

        public void EnqueueException(Exception exception)
        {
            lock (_lock)
            {
                _results.Enqueue(exception);
            }
        }

        public async Task WaitForChecksAsync(int expected)
        {
            while (CheckCount < expected)
            {
                var acquired = await _checkSignal.WaitAsync(WaitTimeoutMilliseconds).ConfigureAwait(false);
                if (!acquired)
                {
                    return;
                }
            }
        }

        public async Task<bool> WasCheckObservedWithinAsync(TimeSpan timeout)
        {
            var acquired = await _checkSignal.WaitAsync(timeout).ConfigureAwait(false);
            if (acquired)
            {
                _checkSignal.Release();
            }

            return acquired;
        }
    }
}
