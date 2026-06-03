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
///     End-to-end tests for <see cref="SocksTunnelHandler" /> covering SOCKS4 and SOCKS5
///     handshakes, success replies, and failure replies for unreachable hosts.
/// </summary>
[NotInParallel]
public sealed class SocksTunnelHandlerTests
{
    private const int UnreachablePort = 1;

    /// <summary>
    ///     Verifies SOCKS4 CONNECT to a reachable target replies with the granted code.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks4Reachable_WritesGrantedReply()
    {
        using var listener = StartTcpListener();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildSocks4Request(IPAddress.Loopback, endpoint.Port);
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsGreaterThanOrEqualTo(8);
        await Assert.That(output[0]).IsEqualTo((byte)0x00);
        await Assert.That(output[1]).IsEqualTo((byte)0x5A);
    }

    /// <summary>
    ///     Verifies SOCKS4 CONNECT to an unreachable target replies with the rejected code.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks4Unreachable_WritesRejectedReply()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var request = BuildSocks4Request(IPAddress.Loopback, UnreachablePort);
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output[1]).IsEqualTo((byte)0x5B);
    }

    /// <summary>
    ///     Verifies SOCKS5 CONNECT to a reachable target performs the greeting + connect
    ///     handshake and replies with success.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5Reachable_WritesSuccessReply()
    {
        using var listener = StartTcpListener();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var greetingBytes = new byte[] { 0x05, 0x01, 0x00 };
        var connectBytes = BuildSocks5ConnectIpv4(IPAddress.Loopback, endpoint.Port);
        await connection.InputWriter.WriteAsync(greetingBytes);
        await connection.InputWriter.WriteAsync(connectBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsGreaterThanOrEqualTo(12);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0x00);
        await Assert.That(output[2]).IsEqualTo((byte)0x05);
        await Assert.That(output[3]).IsEqualTo((byte)0x00);
    }

    /// <summary>
    ///     Verifies SOCKS5 with no acceptable method (only username/password offered) replies
    ///     with 0x05 0xFF and closes.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5OnlyAuthMethod_WritesNoAcceptableReply()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var greetingBytes = new byte[] { 0x05, 0x01, 0x02 };
        await connection.InputWriter.WriteAsync(greetingBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(2);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0xFF);
    }

    /// <summary>
    ///     Verifies SOCKS5 CONNECT to an unreachable target writes a failure reply.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5Unreachable_WritesFailureReply()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var greetingBytes = new byte[] { 0x05, 0x01, 0x00 };
        var connectBytes = BuildSocks5ConnectIpv4(IPAddress.Loopback, UnreachablePort);
        await connection.InputWriter.WriteAsync(greetingBytes);
        await connection.InputWriter.WriteAsync(connectBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output[2]).IsEqualTo((byte)0x05);
        await Assert.That(output[3]).IsEqualTo((byte)0x05);
    }

    /// <summary>
    ///     Verifies SOCKS5 CONNECT to an IPv4 literal bypasses the DNS override resolver:
    ///     an override keyed on the literal must not redirect the connect to a different
    ///     address. Per the handler's documented raw-IP semantics, overrides apply only to
    ///     SOCKS5 domain-name destinations. The override key is intentionally an IP literal
    ///     so that a regression in the resolver path would actively redirect to the
    ///     unreachable 203.0.113.1 address and fail the test.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5IpLiteralWithDnsOverride_BypassesResolver()
    {
        using var listener = StartTcpListener();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var acceptTask = AcceptAndCloseAsync(listener);

        var overrideMap = new DomainNameSystemOverrideMap();
        var entry = new DomainNameSystemOverrideEntry("127.0.0.1", IPAddress.Parse("203.0.113.1")) { IsEnabled = true };
        overrideMap.Add(entry);
        var resolver = new UpstreamHostResolver(overrideMap);
        var handler = new SocksTunnelHandler(NullLogger<SocksTunnelHandler>.Instance, resolver);
        var connection = new StubFullDuplexProxyConnection();
        var greetingBytes = new byte[] { 0x05, 0x01, 0x00 };
        var connectBytes = BuildSocks5ConnectIpv4(IPAddress.Loopback, endpoint.Port);
        await connection.InputWriter.WriteAsync(greetingBytes);
        await connection.InputWriter.WriteAsync(connectBytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await handler.HandleAsync(connection, cancellationSource.Token);
        listener.Stop();
        await acceptTask;
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsGreaterThanOrEqualTo(12);
        await Assert.That(output[2]).IsEqualTo((byte)0x05);
        await Assert.That(output[3]).IsEqualTo((byte)0x00);
        await Assert.That(entry.MatchCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies <see cref="SocksTunnelHandler.CanHandle" /> returns true for SOCKS bytes
    ///     and false for non-SOCKS bytes.
    /// </summary>
    [Test]
    [Arguments(new byte[] { 0x05 }, true)]
    [Arguments(new byte[] { 0x04 }, true)]
    [Arguments(new byte[] { 0x47 }, false)]
    [Arguments(new byte[] { }, false)]
    public async Task CanHandle_VariousInputs_ReturnsExpected(byte[] bytes, bool expected)
    {
        var handler = CreateHandler();
        var sequence = new System.Buffers.ReadOnlySequence<byte>(bytes);

        var actual = handler.CanHandle(sequence);

        await Assert.That(actual).IsEqualTo(expected);
    }

    private static byte[] BuildSocks4Request(IPAddress destinationAddress, int port)
    {
        var addressBytes = destinationAddress.GetAddressBytes();
        var request = new byte[8 + 1];
        request[0] = 0x04;
        request[1] = 0x01;
        request[2] = (byte)((port >> 8) & 0xFF);
        request[3] = (byte)(port & 0xFF);
        request[4] = addressBytes[0];
        request[5] = addressBytes[1];
        request[6] = addressBytes[2];
        request[7] = addressBytes[3];
        request[8] = 0x00;
        return request;
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

    private static SocksTunnelHandler CreateHandler()
    {
        var handler = new SocksTunnelHandler(NullLogger<SocksTunnelHandler>.Instance, null);
        return handler;
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
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
