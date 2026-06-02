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

    /// <summary>
    ///     Verifies that disposing a listener that has never been started works without
    ///     throwing, exercising the null-conditional branches in
    ///     <see cref="SocketProxyListener.Dispose" />.
    /// </summary>
    [Test]
    public async Task Dispose_NeverStarted_DoesNotThrow()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });

        listener.Dispose();

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     Verifies that disposing a listener directly after starting it (without an explicit
    ///     stop) releases the underlying socket, semaphore, and cancellation source, exercising
    ///     the non-null branches in <see cref="SocketProxyListener.Dispose" />.
    /// </summary>
    [Test]
    public async Task Dispose_AfterStartWithoutStop_DoesNotThrow()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });
        await listener.StartAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        listener.Dispose();

        await Assert.That(listener.IsListening).IsTrue();
    }

    /// <summary>
    ///     Verifies that stopping a saturated listener completes the graceful-shutdown contract
    ///     without surfacing a cancellation exception, even when an accepted socket may be
    ///     waiting on the connection-capacity semaphore. Regression test for the accept-loop
    ///     path where <c>SemaphoreSlim.WaitAsync</c> observing cancellation could leak the
    ///     accepted socket and propagate <see cref="OperationCanceledException" /> out of the
    ///     shutdown await.
    /// </summary>
    [Test]
    public async Task StopAsync_WhileSaturatedAndAwaitingCapacity_CompletesGracefully()
    {
        var blockHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstHandlerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort(), MaxConnections = 1 });

        await listener.StartAsync(
            async (_, _) =>
            {
                firstHandlerEntered.TrySetResult();
                await blockHandler.Task;
            },
            CancellationToken.None);

        try
        {
            using var first = new TcpClient();
            await first.ConnectAsync(IPAddress.Loopback, listener.BoundPort!.Value);
            await firstHandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var second = new TcpClient();
            await second.ConnectAsync(IPAddress.Loopback, listener.BoundPort!.Value);

            // Yield repeatedly to give the accept loop a chance to accept the second socket
            // and reach the semaphore wait without using time-based delays (ATXTST004).
            for (var i = 0; i < 100; i++)
            {
                await Task.Yield();
            }
        }
        finally
        {
            // Stop the listener before releasing the handler so that the saturated-accept
            // path (semaphore wait observing cancellation) is reliably exercised. Releasing
            // the handler first could let the semaphore become available before shutdown
            // cancellation, masking the regression scenario under some schedulers.
            await listener.StopAsync(CancellationToken.None);
            blockHandler.TrySetResult();
        }

        await Assert.That(listener.IsListening).IsFalse();
    }

    /// <summary>
    ///     When the connection handler throws a non-cancellation exception, the listener catches
    ///     it and logs without propagating, allowing subsequent connections to proceed normally.
    ///     Exercises the connection-error catch in <see cref="SocketProxyListener" />.
    /// </summary>
    [Test]
    public async Task StartAsync_HandlerThrows_LogsAndContinues()
    {
        var listener = CreateListener(new ProxyOptions { Port = AllocateFreePort() });
        var handlerInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await listener.StartAsync(
            (_, _) =>
            {
                handlerInvoked.TrySetResult();
                throw new InvalidOperationException("intentional handler failure");
            },
            CancellationToken.None);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listener.BoundPort!.Value);
            await handlerInvoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }

        await Assert.That(listener.IsListening).IsFalse();
    }
}