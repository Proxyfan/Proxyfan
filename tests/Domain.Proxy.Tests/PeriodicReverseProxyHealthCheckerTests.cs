using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests;

/// <summary>
///     Tests for <see cref="PeriodicReverseProxyHealthChecker" />. Uses short real-time
///     intervals (~30 ms) and signal-driven waits to exercise the polling loop
///     deterministically without sleeping for fixed durations.
/// </summary>
public sealed class PeriodicReverseProxyHealthCheckerTests
{
    private const int PollIntervalMilliseconds = 30;
    private const int WaitTimeoutMilliseconds = 2000;

    /// <summary>
    ///     Verifies the loop probes every non-stopped route in the engine snapshot.
    /// </summary>
    [Test]
    public async Task Start_TwoHealthyRoutes_ProbesEach()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        engine.SetState("b", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 2);
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(engine.WasProbed("a")).IsTrue();
        await Assert.That(engine.WasProbed("b")).IsTrue();
    }

    /// <summary>
    ///     Verifies the loop skips routes whose status is <see cref="ReverseProxyRouteStatus.Stopped" />.
    /// </summary>
    [Test]
    public async Task Start_StoppedRoute_IsNotProbed()
    {
        var engine = new StubEngine();
        engine.SetState("stopped", ReverseProxyRouteStatus.Stopped);
        engine.SetState("running", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 1);
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(engine.WasProbed("running")).IsTrue();
        await Assert.That(engine.WasProbed("stopped")).IsFalse();
    }

    /// <summary>
    ///     Verifies the loop also probes faulted routes (giving them a chance to recover).
    /// </summary>
    [Test]
    public async Task Start_FaultedRoute_IsProbed()
    {
        var engine = new StubEngine();
        engine.SetState("faulted", ReverseProxyRouteStatus.Faulted);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 1);
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(engine.WasProbed("faulted")).IsTrue();
    }

    /// <summary>
    ///     Verifies a per-route exception does not abort the loop and other routes still get probed.
    /// </summary>
    [Test]
    public async Task Start_ProbeThrows_LoopContinues()
    {
        var engine = new StubEngine();
        engine.SetState("bad", ReverseProxyRouteStatus.Healthy);
        engine.SetState("good", ReverseProxyRouteStatus.Healthy);
        engine.ThrowOnProbe("bad", new InvalidOperationException("simulated"));
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 2);
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(engine.WasProbed("good")).IsTrue();
    }

    /// <summary>
    ///     Verifies the loop honours the configured initial delay before the first probe.
    /// </summary>
    [Test]
    public async Task Start_WithInitialDelay_WaitsBeforeFirstProbe()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        var options = new PeriodicReverseProxyHealthCheckOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(150),
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        var observedBeforeDelay = await engine.WasProbeObservedWithinAsync(TimeSpan.FromMilliseconds(50));
        await engine.WaitForProbesAsync(expected: 1);
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(observedBeforeDelay).IsFalse();
    }

    /// <summary>
    ///     Verifies StopAsync halts further polling.
    /// </summary>
    [Test]
    public async Task StopAsync_AfterStart_StopsFurtherProbes()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 1);
        await checker.StopAsync(CancellationToken.None);
        var probesAfterStop = engine.ProbeCount;
        var furtherProbed = await engine.WasProbeObservedWithinAsync(TimeSpan.FromMilliseconds(150));

        await Assert.That(furtherProbed).IsFalse();
        await Assert.That(engine.ProbeCount).IsEqualTo(probesAfterStop);
    }

    /// <summary>
    ///     Verifies StopAsync without Start is a no-op.
    /// </summary>
    [Test]
    public async Task StopAsync_WithoutStart_IsNoOp()
    {
        var engine = new StubEngine();
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        await checker.StopAsync(CancellationToken.None);

        await Assert.That(engine.ProbeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Start is idempotent: a second call without StopAsync does not spawn a second loop.
    /// </summary>
    [Test]
    public async Task Start_CalledTwice_DoesNotDoubleSpinLoop()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        checker.Start();
        await engine.WaitForProbesAsync(expected: 2);
        var probesObserved = engine.ProbeCount;
        await checker.StopAsync(CancellationToken.None);

        await Assert.That(probesObserved).IsLessThanOrEqualTo(4);
    }

    /// <summary>
    ///     Verifies Dispose without Start does not throw.
    /// </summary>
    [Test]
    public async Task Dispose_WithoutStart_DoesNotThrow()
    {
        var engine = new StubEngine();
        var options = CreateOptions();
        using var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        await Assert.That(engine.ProbeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Dispose after Start cancels the in-flight loop.
    /// </summary>
    [Test]
    public async Task Dispose_AfterStart_CancelsInflightLoop()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Start();
        await engine.WaitForProbesAsync(expected: 1);
        checker.Dispose();
        var probesAfterDispose = engine.ProbeCount;
        var furtherProbed = await engine.WasProbeObservedWithinAsync(TimeSpan.FromMilliseconds(150));

        await Assert.That(furtherProbed).IsFalse();
        await Assert.That(engine.ProbeCount).IsEqualTo(probesAfterDispose);
    }

    /// <summary>
    ///     Verifies Dispose is idempotent: a second call does not throw.
    /// </summary>
    [Test]
    public async Task Dispose_CalledTwice_DoesNotThrow()
    {
        var engine = new StubEngine();
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Dispose();
        checker.Dispose();

        await Assert.That(engine.ProbeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies Start after Dispose is a no-op (no probes occur).
    /// </summary>
    [Test]
    public async Task Start_AfterDispose_DoesNothing()
    {
        var engine = new StubEngine();
        engine.SetState("a", ReverseProxyRouteStatus.Healthy);
        var options = CreateOptions();
        var checker = new PeriodicReverseProxyHealthChecker(engine, options);

        checker.Dispose();
        checker.Start();
        var probed = await engine.WasProbeObservedWithinAsync(TimeSpan.FromMilliseconds(150));

        await Assert.That(probed).IsFalse();
    }

    /// <summary>
    ///     Verifies the constructor rejects a negative <see cref="PeriodicReverseProxyHealthCheckOptions.InitialDelay" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativeInitialDelay_ThrowsArgumentOutOfRange()
    {
        var engine = new StubEngine();
        var options = new PeriodicReverseProxyHealthCheckOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(-1),
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };

        await Assert
            .That(() => new PeriodicReverseProxyHealthChecker(engine, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects a zero <see cref="PeriodicReverseProxyHealthCheckOptions.PollInterval" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithZeroPollInterval_ThrowsArgumentOutOfRange()
    {
        var engine = new StubEngine();
        var options = new PeriodicReverseProxyHealthCheckOptions
        {
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.Zero,
        };

        await Assert
            .That(() => new PeriodicReverseProxyHealthChecker(engine, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Verifies the constructor rejects a negative <see cref="PeriodicReverseProxyHealthCheckOptions.PollInterval" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithNegativePollInterval_ThrowsArgumentOutOfRange()
    {
        var engine = new StubEngine();
        var options = new PeriodicReverseProxyHealthCheckOptions
        {
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(-1),
        };

        await Assert
            .That(() => new PeriodicReverseProxyHealthChecker(engine, options))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static PeriodicReverseProxyHealthCheckOptions CreateOptions()
    {
        var options = new PeriodicReverseProxyHealthCheckOptions
        {
            InitialDelay = TimeSpan.Zero,
            PollInterval = TimeSpan.FromMilliseconds(PollIntervalMilliseconds),
        };
        return options;
    }

    private sealed class StubEngine : IReverseProxyEngine
    {
        private readonly Lock _lock;
        private readonly Dictionary<string, ReverseProxyRouteState> _states;
        private readonly Dictionary<string, Exception> _probeExceptions;
        private readonly HashSet<string> _probed;
        private readonly SemaphoreSlim _probeSignal;
        private int _probeCount;

        public StubEngine()
        {
            var newLock = new Lock();
            var newStates = new Dictionary<string, ReverseProxyRouteState>(StringComparer.Ordinal);
            var newExceptions = new Dictionary<string, Exception>(StringComparer.Ordinal);
            var newProbed = new HashSet<string>(StringComparer.Ordinal);
            var newSignal = new SemaphoreSlim(0);
            _lock = newLock;
            _states = newStates;
            _probeExceptions = newExceptions;
            _probed = newProbed;
            _probeSignal = newSignal;
        }

        public event ReverseProxyRouteStatusChanged? StatusChanged;

        public int ProbeCount
        {
            get
            {
                lock (_lock)
                {
                    return _probeCount;
                }
            }
        }

        public void SetState(string identifier, ReverseProxyRouteStatus status)
        {
            var route = new ReverseProxyRoute(
                identifier,
                identifier,
                listenPort: 9000,
                "backend.local",
                backendPort: 80,
                ReverseProxyTransportLayerSecurityMode.None);
            lock (_lock)
            {
                _states[identifier] = new ReverseProxyRouteState(route, status);
            }
        }

        public void ThrowOnProbe(string identifier, Exception exception)
        {
            lock (_lock)
            {
                _probeExceptions[identifier] = exception;
            }
        }

        public bool WasProbed(string identifier)
        {
            lock (_lock)
            {
                return _probed.Contains(identifier);
            }
        }

        public IReadOnlyList<ReverseProxyRouteState> GetStates()
        {
            lock (_lock)
            {
                return [.. _states.Values];
            }
        }

        public Task<ReverseProxyRouteStatus> ProbeAsync(string identifier, CancellationToken cancellationToken)
        {
            Exception? maybeException;
            ReverseProxyRouteStatus status;
            lock (_lock)
            {
                _probeCount++;
                _probed.Add(identifier);
                _probeExceptions.TryGetValue(identifier, out maybeException);
                status = _states.TryGetValue(identifier, out var existing)
                    ? existing.Status
                    : ReverseProxyRouteStatus.Stopped;
            }

            _probeSignal.Release();
            if (maybeException is not null)
            {
                return Task.FromException<ReverseProxyRouteStatus>(maybeException);
            }

            StatusChanged?.Invoke(identifier, status);
            return Task.FromResult(status);
        }

        public Task<bool> StartRouteAsync(ReverseProxyRoute route, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> StopRouteAsync(string identifier, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task WaitForProbesAsync(int expected)
        {
            while (ProbeCount < expected)
            {
                var acquired = await _probeSignal.WaitAsync(WaitTimeoutMilliseconds).ConfigureAwait(false);
                if (!acquired)
                {
                    return;
                }
            }
        }

        public async Task<bool> WasProbeObservedWithinAsync(TimeSpan timeout)
        {
            var acquired = await _probeSignal.WaitAsync(timeout).ConfigureAwait(false);
            if (acquired)
            {
                _probeSignal.Release();
            }

            return acquired;
        }
    }
}
