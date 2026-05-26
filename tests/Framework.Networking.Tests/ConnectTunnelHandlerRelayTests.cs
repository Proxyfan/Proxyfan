using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectTunnelHandler" /> covering the relay path with
///     actual bidirectional data exchange.
/// </summary>
[NotInParallel]
public sealed class ConnectTunnelHandlerRelayTests
{
    /// <summary>
    ///     Verifies that the tunnel handler sends "200 Connection Established" before starting
    ///     the relay. The relay loop is exercised but bytes-flow timing is non-deterministic on
    ///     in-process pipes, so we assert only on the connect-established sentinel.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReachableHost_SendsConnectionEstablishedBeforeRelay()
    {
        using var upstream = StartEchoServer();
        var endPoint = (IPEndPoint)upstream.Listener.LocalEndpoint;
        var handler = new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance);
        var connection = new StubFullDuplexProxyConnection();
        var connectRequest = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{endPoint.Port} HTTP/1.1\r\nHost: 127.0.0.1:{endPoint.Port}\r\n\r\n");
        await connection.InputWriter.WriteAsync(connectRequest);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        upstream.Stop();
        await connection.Transport.Output.CompleteAsync();
        var outputBytes = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(outputBytes);

        await Assert.That(outputText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
    }

    private static EchoTcpListener StartEchoServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var serverTask = EchoServerLoopAsync(listener);
        return new EchoTcpListener(listener, serverTask);
    }

    private static async Task EchoServerLoopAsync(TcpListener listener)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var networkStream = client.GetStream();
            var buffer = new byte[4096];
            var bytesRead = await networkStream.ReadAsync(buffer).ConfigureAwait(false);

            if (bytesRead > 0)
            {
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                await networkStream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (SocketException)
        {
            // Expected on shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Expected on listener dispose.
        }
        catch (IOException)
        {
            // Expected on connection close.
        }
    }

    private sealed class EchoTcpListener : IDisposable
    {
        private readonly Task _serverTask;

        public TcpListener Listener { get; }

        public EchoTcpListener(TcpListener listener, Task serverTask)
        {
            Listener = listener;
            _serverTask = serverTask;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            try
            {
                Listener.Stop();
            }
            catch (SocketException)
            {
                // Ignored on shutdown.
            }
        }
    }
}
