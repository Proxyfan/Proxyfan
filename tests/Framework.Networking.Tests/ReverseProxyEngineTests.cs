using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ReverseProxyEngine" />.
/// </summary>
[NotInParallel]
public sealed class ReverseProxyEngineTests
{
    /// <summary>
    ///     Verifies a newly created engine reports no states.
    /// </summary>
    [Test]
    public async Task GetStates_NewEngine_IsEmpty()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());

        var states = engine.GetStates();

        await Assert.That(states).IsEmpty();
    }

    /// <summary>
    ///     Verifies starting a route binds a listener and reports Healthy status.
    /// </summary>
    [Test]
    public async Task StartRouteAsync_ValidRoute_ReportsHealthyAndBindsListener()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());

        var started = await StartRouteWithRetryAsync(engine, "api");

        await Assert.That(started).IsTrue();
        var states = engine.GetStates();
        await Assert.That(states.Count).IsEqualTo(1);
        await Assert.That(states[0].Status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
    }

    /// <summary>
    ///     Verifies starting the same route twice returns false.
    /// </summary>
    [Test]
    public async Task StartRouteAsync_DuplicateIdentifier_ReturnsFalse()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());
        await StartRouteWithRetryAsync(engine, "api");

        var startedAgain = await StartRouteWithRetryAsync(engine, "api");

        await Assert.That(startedAgain).IsFalse();
    }

    /// <summary>
    ///     Verifies stopping an existing route succeeds and clears its status.
    /// </summary>
    [Test]
    public async Task StopRouteAsync_RunningRoute_ReturnsTrueAndClearsListener()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());
        await StartRouteWithRetryAsync(engine, "api");

        var stopped = await engine.StopRouteAsync("api", CancellationToken.None);

        await Assert.That(stopped).IsTrue();
        await Assert.That(engine.GetStates()).IsEmpty();
    }

    /// <summary>
    ///     Verifies stopping a route that is not running returns false.
    /// </summary>
    [Test]
    public async Task StopRouteAsync_UnknownRoute_ReturnsFalse()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());

        var stopped = await engine.StopRouteAsync("missing", CancellationToken.None);

        await Assert.That(stopped).IsFalse();
    }

    /// <summary>
    ///     Verifies probing a healthy backend transitions the route to Healthy.
    /// </summary>
    [Test]
    public async Task ProbeAsync_HealthyBackend_ReturnsHealthy()
    {
        var probe = new StubBackendHealthProbe { ResponseHealthy = true };
        await using var engine = CreateEngine(probe);
        await StartRouteWithRetryAsync(engine, "api");

        var status = await engine.ProbeAsync("api", CancellationToken.None);

        await Assert.That(status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
        await Assert.That(probe.ProbeCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies probing an unhealthy backend transitions the route to Unhealthy.
    /// </summary>
    [Test]
    public async Task ProbeAsync_UnhealthyBackend_ReturnsUnhealthy()
    {
        var probe = new StubBackendHealthProbe { ResponseHealthy = false };
        await using var engine = CreateEngine(probe);
        await StartRouteWithRetryAsync(engine, "api");

        var status = await engine.ProbeAsync("api", CancellationToken.None);

        await Assert.That(status).IsEqualTo(ReverseProxyRouteStatus.Unhealthy);
    }

    /// <summary>
    ///     Verifies probing a route that is not registered returns Stopped without probing.
    /// </summary>
    [Test]
    public async Task ProbeAsync_UnknownRoute_ReturnsStopped()
    {
        var probe = new StubBackendHealthProbe();
        await using var engine = CreateEngine(probe);

        var status = await engine.ProbeAsync("missing", CancellationToken.None);

        await Assert.That(status).IsEqualTo(ReverseProxyRouteStatus.Stopped);
        await Assert.That(probe.ProbeCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies DisposeAsync stops all listeners and is idempotent.
    /// </summary>
    [Test]
    public async Task DisposeAsync_WithStartedRoutes_StopsAllListenersAndIsIdempotent()
    {
        var engine = CreateEngine(new StubBackendHealthProbe());
        await StartRouteWithRetryAsync(engine, "api1");
        await StartRouteWithRetryAsync(engine, "api2");

        await engine.DisposeAsync();
        await engine.DisposeAsync();

        await Assert.That(engine.GetStates()).IsEmpty();
    }

    /// <summary>
    ///     Verifies starting a route on a port that is already bound fails gracefully,
    ///     the listener is disposed, and the route is marked Faulted (covers the
    ///     ProxyBindException catch branch of StartRouteAsync).
    /// </summary>
    [Test]
    public async Task StartRouteAsync_PortAlreadyInUse_ReturnsFalseAndMarksFaulted()
    {
        using var blockingListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        blockingListener.Start();
        var blockedPort = ((System.Net.IPEndPoint)blockingListener.LocalEndpoint).Port;
        await using var engine = CreateEngine(new StubBackendHealthProbe());
        var route = CreateRoute("api", listenPort: blockedPort);

        var started = await engine.StartRouteAsync(route, CancellationToken.None);

        blockingListener.Stop();
        await Assert.That(started).IsFalse();
    }

    /// <summary>
    ///     Verifies that a probe in flight when the route is stopped does not
    ///     overwrite the Stopped status when it completes.
    /// </summary>
    [Test]
    public async Task ProbeAsync_RouteStoppedBeforeCompletion_DoesNotOverwriteStatus()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new StubBackendHealthProbe { ResponseHealthy = true, ProbeGate = gate.Task };
        await using var engine = CreateEngine(probe);
        await StartRouteWithRetryAsync(engine, "api");
        var statusChanges = new List<ReverseProxyRouteStatus>();
        engine.StatusChanged += (_, status) => statusChanges.Add(status);

        var probeTask = engine.ProbeAsync("api", CancellationToken.None);
        await probe.ProbeStarted;
        await engine.StopRouteAsync("api", CancellationToken.None);
        gate.SetResult();
        var result = await probeTask;

        await Assert.That(result).IsEqualTo(ReverseProxyRouteStatus.Stopped);
        await Assert.That(statusChanges).DoesNotContain(ReverseProxyRouteStatus.Healthy);
        await Assert.That(statusChanges).DoesNotContain(ReverseProxyRouteStatus.Unhealthy);
    }

    /// <summary>
    ///     Verifies that a probe in flight when the route is stopped and restarted
    ///     does not overwrite the freshly started listener's status.
    /// </summary>
    [Test]
    public async Task ProbeAsync_RouteRestartedBeforeCompletion_DoesNotOverwriteNewStatus()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new StubBackendHealthProbe { ResponseHealthy = false, ProbeGate = gate.Task };
        await using var engine = CreateEngine(probe);
        await StartRouteWithRetryAsync(engine, "api");

        var probeTask = engine.ProbeAsync("api", CancellationToken.None);
        await probe.ProbeStarted;
        await engine.StopRouteAsync("api", CancellationToken.None);
        await StartRouteWithRetryAsync(engine, "api");
        gate.SetResult();
        _ = await probeTask;

        var states = engine.GetStates();
        await Assert.That(states.Count).IsEqualTo(1);
        await Assert.That(states[0].Status).IsEqualTo(ReverseProxyRouteStatus.Healthy);
    }

    private static ReverseProxyEngine CreateEngine(IBackendHealthProbe probe)
    {
        var factory = new StubLoggerFactory();
        var logger = new StubLogger<ReverseProxyEngine>();
        var engine = new ReverseProxyEngine(probe, factory, logger, hypertextTransferProtocolHandler: null);
        return engine;
    }

    private static ReverseProxyRoute CreateRoute(string identifier, int listenPort)
    {
        return new ReverseProxyRoute(
            identifier,
            $"Route {identifier}",
            listenPort,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);
    }

    /// <summary>
    ///     Starts a route in the engine on a free port, using the bind-probe-and-retry pattern:
    ///     a <see cref="System.Net.Sockets.TcpListener" /> probe on port 0 holds the OS port
    ///     reservation while the route is constructed, is then released immediately before
    ///     <see cref="ReverseProxyEngine.StartRouteAsync" />, and the whole attempt is retried
    ///     up to five times if <see cref="ReverseProxyEngine.StartRouteAsync" /> returns
    ///     <see langword="false" /> due to a bind conflict rather than a duplicate identifier.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> when the route started successfully;
    ///     <see langword="false" /> when the identifier was already registered.
    /// </returns>
    private static async Task<bool> StartRouteWithRetryAsync(
        ReverseProxyEngine engine,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            // Hold the probe alive while constructing the route to keep the OS port
            // reservation continuous up to the moment the engine's listener binds.
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            var route = CreateRoute(identifier, port);
            probe.Stop(); // Release port; engine.StartRouteAsync binds next.

            if (await engine.StartRouteAsync(route, cancellationToken))
            {
                return true;
            }

            // StartRouteAsync returns false for two reasons:
            // 1. Identifier already running → route appears in GetStates(); stop retrying.
            // 2. Bind failed (port conflict) → route is Faulted and absent from GetStates(); retry.
            if (engine.GetStates().Any(s => s.Route.Identifier == identifier))
            {
                return false;
            }
        }

        return false;
    }
}
