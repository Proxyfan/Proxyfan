using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;
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
        var route = CreateRoute("api", listenPort: 0);

        var started = await engine.StartRouteAsync(route, CancellationToken.None);

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
        var route = CreateRoute("api", listenPort: 0);
        await engine.StartRouteAsync(route, CancellationToken.None);

        var startedAgain = await engine.StartRouteAsync(route, CancellationToken.None);

        await Assert.That(startedAgain).IsFalse();
    }

    /// <summary>
    ///     Verifies stopping an existing route succeeds and clears its status.
    /// </summary>
    [Test]
    public async Task StopRouteAsync_RunningRoute_ReturnsTrueAndClearsListener()
    {
        await using var engine = CreateEngine(new StubBackendHealthProbe());
        var route = CreateRoute("api", listenPort: 0);
        await engine.StartRouteAsync(route, CancellationToken.None);

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
        var route = CreateRoute("api", listenPort: 0);
        await engine.StartRouteAsync(route, CancellationToken.None);

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
        var route = CreateRoute("api", listenPort: 0);
        await engine.StartRouteAsync(route, CancellationToken.None);

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
        var route1 = CreateRoute("api1", listenPort: 0);
        var route2 = CreateRoute("api2", listenPort: 0);
        await engine.StartRouteAsync(route1, CancellationToken.None);
        await engine.StartRouteAsync(route2, CancellationToken.None);

        await engine.DisposeAsync();
        await engine.DisposeAsync();

        await Assert.That(engine.GetStates()).IsEmpty();
    }

    private static ReverseProxyEngine CreateEngine(IBackendHealthProbe probe)
    {
        var factory = new StubLoggerFactory();
        var logger = new StubLogger<ReverseProxyEngine>();
        return new ReverseProxyEngine(probe, factory, logger);
    }

    private static ReverseProxyRoute CreateRoute(string identifier, int listenPort)
    {
        return new ReverseProxyRoute(
            identifier,
            $"Route {identifier}",
            listenPort: listenPort == 0 ? GetFreePort() : listenPort,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
