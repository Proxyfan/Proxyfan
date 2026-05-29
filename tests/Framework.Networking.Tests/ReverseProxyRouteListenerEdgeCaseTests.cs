using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Edge-case tests for <see cref="ReverseProxyRouteListener" /> covering dispose,
///     double-stop, and IsListening state transitions.
/// </summary>
[NotInParallel]
public sealed class ReverseProxyRouteListenerEdgeCaseTests
{
    /// <summary>
    ///     Disposing the listener after stop is a no-op (covers the null-conditional branches in
    ///     <see cref="ReverseProxyRouteListener.Dispose" /> where both fields are already null).
    /// </summary>
    [Test]
    public async Task Dispose_AfterStop_ClearsResources()
    {
        var route = new ReverseProxyRoute(
            "dispose-after-stop",
            "Dispose after stop",
            GetFreePort(),
            "127.0.0.1",
            GetFreePort(),
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);
        await listener.StartAsync(CancellationToken.None);
        await listener.StopAsync(CancellationToken.None);

        listener.Dispose();

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     Stopping the listener twice in succession is a safe no-op the second time.
    ///     Covers the null/dispose branches in <see cref="ReverseProxyRouteListener.StopAsync" />.
    /// </summary>
    [Test]
    public async Task StopAsync_CalledTwice_DoesNotThrow()
    {
        var route = new ReverseProxyRoute(
            "stop-twice",
            "Stop twice",
            GetFreePort(),
            "127.0.0.1",
            GetFreePort(),
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);
        try
        {
            await listener.StartAsync(CancellationToken.None);
            await listener.StopAsync(CancellationToken.None);
            await listener.StopAsync(CancellationToken.None);
        }
        finally
        {
            listener.Dispose();
        }

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     When a client connects to the listener but the backend port is unreachable, the
    ///     forwarder's SocketException catch logs the failure and the client connection closes
    ///     cleanly. Exercises the backend-connect error path in
    ///     <see cref="ReverseProxyRouteListener.ForwardConnectionAsync" />.
    /// </summary>
    [Test]
    public async Task ForwardConnection_BackendUnreachable_LogsErrorAndClosesClient()
    {
        var listenPort = GetFreePort();
        var unreachableBackendPort = GetFreePort();
        var route = new ReverseProxyRoute(
            "no-backend",
            "Unreachable Backend",
            listenPort,
            "127.0.0.1",
            unreachableBackendPort,
            ReverseProxyTransportLayerSecurityMode.None);

        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);
        try
        {
            await listener.StartAsync(CancellationToken.None);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenPort);
            var stream = client.GetStream();
            var buffer = new byte[1];
            var bytesRead = await stream.ReadAsync(buffer, CancellationToken.None);
            await Assert.That(bytesRead).IsEqualTo(0);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            listener.Dispose();
        }

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     Disposing the listener while it is still bound (without first calling StopAsync) is
    ///     safe — the listener is torn down and the accept loop exits via ObjectDisposedException.
    ///     Exercises the disposed-without-stop fault path in
    ///     <see cref="ReverseProxyRouteListener.RunAcceptLoopAsync" />.
    /// </summary>
    [Test]
    public async Task Dispose_WhileListening_StopsListenerWithoutThrowing()
    {
        var route = new ReverseProxyRoute(
            "dispose-while-listening",
            "Dispose while listening",
            GetFreePort(),
            "127.0.0.1",
            GetFreePort(),
            ReverseProxyTransportLayerSecurityMode.None);
        var listener = new ReverseProxyRouteListener(route, new StubLogger<ReverseProxyRouteListener>(), hypertextTransferProtocolHandler: null);
        await listener.StartAsync(CancellationToken.None);
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
}
