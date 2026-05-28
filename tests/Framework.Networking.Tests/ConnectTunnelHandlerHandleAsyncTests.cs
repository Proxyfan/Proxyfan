using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Behavioral tests for <see cref="ConnectTunnelHandler.HandleAsync" /> covering tunnel
///     establishment, error paths, and relay completion.
/// </summary>
[NotInParallel]
public sealed class ConnectTunnelHandlerHandleAsyncTests
{
    private const int UnreachablePort = 1;

    /// <summary>
    ///     When the upstream host is unreachable, the handler must respond with HTTP 502 Bad Gateway.
    /// </summary>
    [Test]
    public async Task HandleAsync_UnreachableHost_WritesBadGatewayResponse()
    {
        var handler = new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance, null);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{UnreachablePort} HTTP/1.1\r\nHost: 127.0.0.1:{UnreachablePort}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     When the upstream host is reachable, the handler must respond with HTTP 200 Connection
    ///     Established and then relay traffic bidirectionally until the connections close.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReachableHost_WritesConnectionEstablishedAndRelays()
    {
        using var listener = StartTcpListener();
        var endPoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        var handler = new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance, null);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT 127.0.0.1:{endPoint.Port} HTTP/1.1\r\nHost: 127.0.0.1:{endPoint.Port}\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 200", StringComparison.Ordinal)).IsTrue();
    }

    private static TcpListener StartTcpListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static async Task AcceptAndCloseAsync(TcpListener listener)
    {
        try
        {
            using var client = await listener.AcceptTcpClientAsync();
        }
        catch (SocketException)
        {
            // The client may close before accept completes; ignore.
        }
        catch (ObjectDisposedException)
        {
            // The listener may be disposed; ignore.
        }
    }
}