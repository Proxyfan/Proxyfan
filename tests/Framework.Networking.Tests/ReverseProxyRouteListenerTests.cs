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
        var backendPort = GetFreePort();
        using var echoCancellation = new CancellationTokenSource();
        var echoServerTask = RunEchoServerAsync(backendPort, echoCancellation.Token);

        var listenPort = GetFreePort();
        var route = new ReverseProxyRoute(
            "echo",
            "Echo route",
            listenPort,
            "127.0.0.1",
            backendPort,
            ReverseProxyTransportLayerSecurityMode.None);

        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>());
        try
        {
            await listener.StartAsync(CancellationToken.None);

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
        var port = GetFreePort();
        using var blocker = new TcpListener(IPAddress.Loopback, port);
        blocker.Start();

        var route = new ReverseProxyRoute(
            "conflict",
            "Conflict",
            port,
            "127.0.0.1",
            65500,
            ReverseProxyTransportLayerSecurityMode.None);

        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>());
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
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>());

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
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>());

        await Assert.That(listener.GetRoute()).IsSameReferenceAs(route);
        listener.Dispose();
    }

    private static int GetFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
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
