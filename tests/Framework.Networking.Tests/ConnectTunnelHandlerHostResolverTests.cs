using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Behavioral tests for <see cref="ConnectTunnelHandler" /> wiring of
///     <see cref="UpstreamHostResolver" />: when a DNS override exists for the requested host,
///     the handler must dial the override IP rather than the original (unroutable) hostname.
///     This proves the DNS spoofing UI is wired end-to-end into the outbound connection path.
/// </summary>
[NotInParallel]
public sealed class ConnectTunnelHandlerHostResolverTests
{
    /// <summary>
    ///     When a DNS override redirects the CONNECT target to a reachable loopback listener,
    ///     the handler must establish the tunnel and reply with HTTP 200 Connection Established.
    /// </summary>
    [Test]
    public async Task HandleAsync_HostResolverRedirectsToLoopback_TunnelEstablished()
    {
        using var listener = StartTcpListener();
        var endPoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        var overrideMap = new DomainNameSystemOverrideMap();
        overrideMap.Add(new DomainNameSystemOverrideEntry("redirected.invalid", IPAddress.Loopback));
        var resolver = new UpstreamHostResolver(overrideMap);

        var handler = new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance, resolver);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes($"CONNECT redirected.invalid:{endPoint.Port} HTTP/1.1\r\nHost: redirected.invalid:{endPoint.Port}\r\n\r\n");
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

    /// <summary>
    ///     When the DNS override map has no entry for the requested host, the handler must fall
    ///     through to the original hostname. With an unroutable `.invalid` TLD this MUST fail
    ///     fast with HTTP 502 Bad Gateway rather than silently succeeding.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoOverride_FallsThroughToOriginalHostname()
    {
        var overrideMap = new DomainNameSystemOverrideMap();
        overrideMap.Add(new DomainNameSystemOverrideEntry("other.invalid", IPAddress.Loopback));
        var resolver = new UpstreamHostResolver(overrideMap);

        var handler = new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance, resolver);
        var connection = new StubFullDuplexProxyConnection();
        var requestBytes = Encoding.ASCII.GetBytes("CONNECT unrouted.invalid:443 HTTP/1.1\r\nHost: unrouted.invalid:443\r\n\r\n");
        await connection.InputWriter.WriteAsync(requestBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();
        var outputText = Encoding.ASCII.GetString(output);

        await Assert.That(outputText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
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
