using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;
using Proxyfan.Framework.Networking.Tests.Stubs;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="SocketProxyListener" />.
/// </summary>
[NotInParallel]
public sealed class SocketProxyListenerTests
{
    private static SocketProxyListener CreateListener(ProxyOptions options)
    {
        var monitor = new StubOptionsMonitor<ProxyOptions>(options);
        var logger = new StubLogger<SocketProxyListener>();
        return new SocketProxyListener(monitor, logger);
    }

    private static int AllocateFreePort()
    {
        using var temp = new TcpListener(IPAddress.Loopback, 0);
        temp.Start();
        var port = ((IPEndPoint)temp.LocalEndpoint).Port;
        temp.Stop();
        return port;
    }

    /// <summary>
    ///     Verifies that binding to an already-occupied port throws <see cref="ProxyBindException" />.
    /// </summary>
    [Test]
    public async Task StartAsync_PortInUse_ThrowsProxyBindException()
    {
        var port = AllocateFreePort();

        using var blockingSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        blockingSocket.DualMode = true;
        blockingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
        blockingSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
        blockingSocket.Listen();

        var listener = CreateListener(new ProxyOptions { Port = port });

        try
        {
            await Assert.That(
                async () => await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None)
            ).Throws<ProxyBindException>();
        }
        finally
        {
            blockingSocket.Dispose();
        }
    }

    /// <summary>
    ///     Verifies that starting the listener sets <see cref="SocketProxyListener.IsListening" /> to
    ///     <see langword="true" />.
    /// </summary>
    [Test]
    public async Task StartAsync_ValidPort_SetsIsListeningToTrue()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });

        await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        try
        {
            await Assert.That(listener.IsListening).IsTrue();
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    ///     Verifies that starting the listener populates <see cref="SocketProxyListener.BoundPort" />.
    /// </summary>
    [Test]
    public async Task StartAsync_ValidPort_SetsBoundPort()
    {
        var port = AllocateFreePort();
        var listener = CreateListener(new ProxyOptions { Port = port });

        await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        try
        {
            await Assert.That(listener.BoundPort).IsEqualTo(port);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    ///     Verifies that an accepted connection triggers the callback.
    /// </summary>
    [Test]
    public async Task StartAsync_ThenConnect_CallbackInvoked()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });

        await listener.StartAsync(
            (_, _) =>
            {
                tcs.TrySetResult();
                return Task.CompletedTask;
            },
            CancellationToken.None);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listener.BoundPort!.Value);
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }

        await Assert.That(tcs.Task.IsCompleted).IsTrue();
    }

    /// <summary>
    ///     Verifies that stopping the listener clears <see cref="SocketProxyListener.BoundPort" />.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenListening_ClearsBoundPort()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });
        await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        await listener.StopAsync(CancellationToken.None);

        await Assert.That(listener.BoundPort).IsNull();
    }

    /// <summary>
    ///     Verifies that stopping the listener sets <see cref="SocketProxyListener.IsListening" /> to
    ///     <see langword="false" />.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenListening_SetsIsListeningToFalse()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });
        await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        await listener.StopAsync(CancellationToken.None);

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     Verifies that calling <see cref="SocketProxyListener.StopAsync" /> when not listening does not throw.
    /// </summary>
    [Test]
    public async Task StopAsync_WhenNotListening_DoesNotThrow()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });
        await listener.StopAsync(CancellationToken.None);
    }
}