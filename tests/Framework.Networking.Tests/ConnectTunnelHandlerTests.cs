using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Framework.Networking.Tests.Stubs;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectTunnelHandler" />.
/// </summary>
public sealed class ConnectTunnelHandlerTests
{
    private static ConnectTunnelHandler CreateHandler()
    {
        return new ConnectTunnelHandler(NullLogger<ConnectTunnelHandler>.Instance);
    }

    /// <summary>
    ///     Verifies that exactly 8 bytes matching "CONNECT " return true.
    /// </summary>
    [Test]
    public async Task CanHandle_ExactConnectPrefix_ReturnsTrue()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CONNECT ");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that a longer sequence starting with "CONNECT " returns true.
    /// </summary>
    [Test]
    public async Task CanHandle_LongerConnectRequest_ReturnsTrue()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CONNECT api.example.com:443 HTTP/1.1\r\n");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    ///     Verifies that bytes starting with "GET " return false.
    /// </summary>
    [Test]
    public async Task CanHandle_GetRequestBytes_ReturnsFalse()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("GET / HTT");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that fewer than 8 bytes return false.
    /// </summary>
    [Test]
    public async Task CanHandle_InsufficientBytes_ReturnsFalse()
    {
        var handler = CreateHandler();
        var bytes = Encoding.ASCII.GetBytes("CONN");
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that an empty sequence returns false.
    /// </summary>
    [Test]
    public async Task CanHandle_EmptySequence_ReturnsFalse()
    {
        var handler = CreateHandler();
        var sequence = ReadOnlySequence<byte>.Empty;

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that SOCKS4 bytes (0x04) return false.
    /// </summary>
    [Test]
    public async Task CanHandle_Socks4Bytes_ReturnsFalse()
    {
        var handler = CreateHandler();
        var bytes = new byte[] { 0x04, 0x01, 0x00, 0x50, 0x7F, 0x00, 0x00, 0x01 };
        var sequence = new ReadOnlySequence<byte>(bytes);

        var result = handler.CanHandle(sequence);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    ///     Verifies that a malformed CONNECT request (no end of headers) results
    ///     in an error response being written to the client.
    /// </summary>
    [Test]
    public async Task HandleAsync_MalformedConnectRequest_WritesErrorResponse()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();

        var request = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1\r\n");
        await connection.InputWriter.WriteAsync(request);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);

        await connection.Transport.Output.CompleteAsync();
        var response = await connection.ReadAllOutputAsync();

        await Assert.That(response.Length > 0).IsTrue();
        var responseText = Encoding.ASCII.GetString(response);
        await Assert.That(responseText.StartsWith("HTTP/1.1 502", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    ///     Verifies that when the connection closes with no data, the handler returns without crashing.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyInput_ReturnsWithoutCrashing()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();

        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
    }
}
