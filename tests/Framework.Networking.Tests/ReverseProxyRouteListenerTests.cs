using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Integration tests for <see cref="ReverseProxyRouteListener" /> that bind real ports
///     and forward bytes through to a local TCP echo server.
/// </summary>
[NotInParallel]
public sealed class ReverseProxyRouteListenerTests
{
    /// <summary>
    ///     Verifies a client request is forwarded to the backend and the echoed bytes return.
    /// </summary>
    [Test]
    public async Task PumpAsync_ConnectedClient_EchoesBytesEndToEnd()
    {
        // Keep the backend probe alive until RunEchoServerAsync binds to minimise the
        // close-then-rebind race window for the backend port.
        var backendProbe = new TcpListener(IPAddress.Loopback, 0);
        backendProbe.Start();
        var backendPort = ((IPEndPoint)backendProbe.LocalEndpoint).Port;
        backendProbe.Stop(); // Release immediately before the echo server binds.

        using var echoCancellation = new CancellationTokenSource();
        var echoServerTask = RunEchoServerAsync(backendPort, echoCancellation.Token);

        // BindRouteListenerAsync keeps the listen-port probe alive while constructing
        // the listener, then releases it just before StartAsync (bind-and-pass + retry).
        var (listener, listenPort) = await BindRouteListenerAsync(
            port => new ReverseProxyRouteListener(
                new ReverseProxyRoute("echo", "Echo route", port, "127.0.0.1", backendPort, ReverseProxyTransportLayerSecurityMode.None),
                new StubLogger<ReverseProxyRouteListener>(),
                hypertextTransferProtocolHandler: null));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            using var stream = client.GetStream();

            var payload = Encoding.ASCII.GetBytes("hello");
            await stream.WriteAsync(payload);

            var buffer = new byte[payload.Length];
            var read = await ReadFullyAsync(stream, buffer, TimeSpan.FromSeconds(5));

            await Assert.That(read).IsEqualTo(payload.Length);
            await Assert.That(Encoding.ASCII.GetString(buffer)).IsEqualTo("hello");
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
            await echoCancellation.CancelAsync();
            try
            {
                await echoServerTask;
            }
            catch (OperationCanceledException ex)
            {
                _ = ex;
            }
        }
    }

    /// <summary>
    ///     Verifies starting twice on the same port returns a bind exception.
    /// </summary>
    [Test]
    public async Task StartAsync_PortAlreadyInUse_ThrowsProxyBindException()
    {
        // Bind the blocker on port 0 first so the OS assigns the port, then read it back.
        // This eliminates the close-then-rebind race: the blocker IS the occupier from the
        // very start — no probe-close-then-steal window.
        using var blocker = new TcpListener(IPAddress.Loopback, 0);
        blocker.Start();
        var port = ((IPEndPoint)blocker.LocalEndpoint).Port;

        var route = new ReverseProxyRoute(
            "conflict",
            "Conflict",
            port,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);

        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);
        try
        {
            await Assert.That(async () => await listener.StartAsync(CancellationToken.None)).Throws<ProxyBindException>();
        }
        finally
        {
            listener.Dispose();
            blocker.Stop();
        }
    }

    /// <summary>
    ///     Verifies stopping a listener that was never started is a no-op.
    /// </summary>
    [Test]
    public async Task StopAsync_NotStarted_NoOps()
    {
        var route = new ReverseProxyRoute(
            "x",
            "X",
            65501,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);

        await listener.StopAsync(CancellationToken.None);

        await Assert.That(listener.IsListening).IsFalse();
        listener.Dispose();
    }

    /// <summary>
    ///     Verifies GetRoute returns the route the listener was constructed with.
    /// </summary>
    [Test]
    public async Task GetRoute_AfterConstruction_ReturnsSameRoute()
    {
        var route = new ReverseProxyRoute(
            "x",
            "X",
            65502,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);

        await Assert.That(listener.GetRoute()).IsSameReferenceAs(route);
        listener.Dispose();
    }

    /// <summary>
    ///     Starts a <see cref="ReverseProxyRouteListener" /> on a free port using the
    ///     bind-probe-and-retry pattern: a <see cref="TcpListener" /> probe on port 0 holds the
    ///     OS port reservation while the production listener is constructed, is then released
    ///     immediately before <see cref="ReverseProxyRouteListener.StartAsync" />, and the
    ///     whole attempt is retried up to five times on <see cref="ProxyBindException" />.
    /// </summary>
    /// <param name="createListener">
    ///     Factory that receives the probed free port and produces a
    ///     <see cref="ReverseProxyRouteListener" /> configured to listen on that port.
    /// </param>
    /// <returns>The started listener and the port it successfully bound to.</returns>
    private static async Task<(ReverseProxyRouteListener Listener, int ListenPort)> BindRouteListenerAsync(
        Func<int, ReverseProxyRouteListener> createListener)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            // Hold the probe alive while constructing the route/listener so the OS port
            // reservation is continuous up to the moment the production socket binds.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            var listener = createListener(port);
            probe.Stop(); // Release port; production socket binds next.
            try
            {
                await listener.StartAsync(CancellationToken.None);
                return (listener, port);
            }
            catch (ProxyBindException)
            {
                listener.Dispose();
                if (attempt == 4)
                {
                    throw new InvalidOperationException("Unable to bind a free listen port after 5 attempts.");
                }
            }
        }

        throw new InvalidOperationException("Unable to bind a free listen port after 5 attempts.");
    }

    private static async Task<int> ReadFullyAsync(NetworkStream stream, byte[] buffer, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    private static async Task RunEchoServerAsync(int port, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = EchoOneAsync(client, cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task EchoOneAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, cancellationToken);
                    if (read == 0)
                    {
                        return;
                    }

                    await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ = ex;
            }
        }
    }
}
