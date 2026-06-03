using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Domain.DomainNameSystemSpoofing;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Behavioral tests for <see cref="SocksTunnelHandler" /> wiring of
///     <see cref="UpstreamHostResolver" />: DNS overrides apply only to SOCKS5
///     domain-name destinations and must NOT redirect SOCKS5-by-IP destinations.
/// </summary>
[NotInParallel]
public sealed class SocksTunnelHandlerHostResolverTests
{
    /// <summary>
    ///     When a SOCKS5 client targets an IPv4 literal, the host resolver must be bypassed
    ///     even if an override entry exists whose key matches that literal — IP-literal
    ///     destinations preserve raw-IP semantics.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5Ipv4LiteralWithMatchingOverride_BypassesResolver()
    {
        using var listener = StartTcpListener();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        // Override entry keyed by the IP literal — if the handler incorrectly forwards
        // IP destinations through the resolver, the connection would be redirected to
        // an unrouted address and the listener would never accept.
        var overrideMap = new DomainNameSystemOverrideMap();
        overrideMap.Add(new DomainNameSystemOverrideEntry(IPAddress.Loopback.ToString(), IPAddress.Parse("203.0.113.1")));
        var resolver = new UpstreamHostResolver(overrideMap);

        var handler = new SocksTunnelHandler(NullLogger<SocksTunnelHandler>.Instance, resolver);
        var connection = new StubFullDuplexProxyConnection();
        var greeting = new byte[] { 0x05, 0x01, 0x00 };
        var connectBytes = BuildSocks5ConnectIpv4(IPAddress.Loopback, endpoint.Port);
        await connection.InputWriter.WriteAsync(greeting);
        await connection.InputWriter.WriteAsync(connectBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsGreaterThanOrEqualTo(4);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0x00);
        await Assert.That(output[2]).IsEqualTo((byte)0x05);
        await Assert.That(output[3]).IsEqualTo((byte)0x00);
    }

    private static byte[] BuildSocks5ConnectIpv4(IPAddress destinationAddress, int port)
    {
        var addressBytes = destinationAddress.GetAddressBytes();
        var request = new byte[10];
        request[0] = 0x05;
        request[1] = 0x01;
        request[2] = 0x00;
        request[3] = 0x01;
        request[4] = addressBytes[0];
        request[5] = addressBytes[1];
        request[6] = addressBytes[2];
        request[7] = addressBytes[3];
        request[8] = (byte)((port >> 8) & 0xFF);
        request[9] = (byte)(port & 0xFF);
        return request;
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
