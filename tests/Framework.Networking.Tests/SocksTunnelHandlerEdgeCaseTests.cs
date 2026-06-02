using Microsoft.Extensions.Logging.Abstractions;
using Proxyfan.Framework.Networking.Tests.Stubs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Edge-case tests for <see cref="SocksTunnelHandler" /> covering the null-version,
///     short-buffer, and malformed-request early-return branches that are not exercised by
///     the happy-path end-to-end tests in <see cref="SocksTunnelHandlerTests" />.
/// </summary>
[NotInParallel]
public sealed class SocksTunnelHandlerEdgeCaseTests
{
    /// <summary>
    ///     Verifies that the handler returns silently when the client completes the input
    ///     transport without sending any bytes, exercising the "version is null" early-return
    ///     branch in <see cref="SocksTunnelHandler.HandleAsync" />.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoBytesAndCompletedInput_ReturnsWithoutWritingOutput()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a SOCKS4 CONNECT request whose USERID is not terminated with a NUL
    ///     byte before the input pipe completes is treated as malformed: the handler returns
    ///     without ever tunneling. Exercises the request-is-null branch in
    ///     <see cref="SocksTunnelHandler" />.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks4MissingUserIdTerminator_ReturnsWithoutWritingOutput()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        var bytes = new byte[]
        {
            0x04, 0x01, 0x00, 0x50,
            0x7F, 0x00, 0x00, 0x01,
            0x41,
        };
        await connection.InputWriter.WriteAsync(bytes);
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a SOCKS5 client that sends only one byte (insufficient for the
    ///     greeting header) and then closes the input causes the handler to bail out
    ///     without writing any selection or rejection.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5GreetingShorterThanTwoBytes_ReturnsWithoutWritingOutput()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(new byte[] { 0x05 });
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a SOCKS5 greeting which declares more methods than are actually
    ///     present in the buffer (method count exceeds remaining bytes) causes the parser to
    ///     return null and the handler to bail out, exercising the greeting-is-null branch
    ///     in <see cref="SocksTunnelHandler" />.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5GreetingMethodCountExceedsBuffer_ReturnsWithoutWritingOutput()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(new byte[] { 0x05, 0x05 });
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a SOCKS5 connect request shorter than four bytes (after a successful
    ///     greeting) causes the handler to return without invoking the SOCKS5 connect parser,
    ///     exercising the short-buffer guard in <see cref="SocksTunnelHandler" />.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5ConnectShorterThanFourBytes_WritesOnlyMethodSelection()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(new byte[] { 0x05, 0x01, 0x00 });
        await connection.InputWriter.WriteAsync(new byte[] { 0x05, 0x01, 0x00 });
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(2);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0x00);
    }

    /// <summary>
    ///     Verifies that a SOCKS5 connect request with a domain-name address type whose
    ///     declared length exceeds the buffered bytes causes the parser to return null and
    ///     the handler to bail out without writing a connect reply.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5ConnectIncompleteDomain_WritesOnlyMethodSelection()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(new byte[] { 0x05, 0x01, 0x00 });
        await connection.InputWriter.WriteAsync(new byte[]
        {
            0x05, 0x01, 0x00, 0x03,
            0x40,
            0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65,
        });
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(2);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0x00);
    }

    /// <summary>
    ///     Verifies that a SOCKS5 CONNECT request with a non-zero reserved byte (RFC 1928 § 4
    ///     requires 0x00) causes the handler to emit a SOCKS5 general failure reply after the
    ///     no-auth method selection, instead of tunneling.
    /// </summary>
    [Test]
    public async Task HandleAsync_Socks5ConnectNonZeroReservedByte_WritesMethodSelectionAndFailureReply()
    {
        var handler = CreateHandler();
        var connection = new StubFullDuplexProxyConnection();
        await connection.InputWriter.WriteAsync(new byte[] { 0x05, 0x01, 0x00 });
        await connection.InputWriter.WriteAsync(new byte[]
        {
            0x05, 0x01, 0xFF, 0x01,
            192, 168, 1, 1,
            0x01, 0xBB,
        });
        await connection.InputWriter.CompleteAsync();

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.HandleAsync(connection, cancellationSource.Token);
        await connection.Transport.Output.CompleteAsync();
        var output = await connection.ReadAllOutputAsync();

        await Assert.That(output.Length).IsEqualTo(12);
        await Assert.That(output[0]).IsEqualTo((byte)0x05);
        await Assert.That(output[1]).IsEqualTo((byte)0x00);
        await Assert.That(output[2]).IsEqualTo((byte)0x05);
        await Assert.That(output[3]).IsEqualTo((byte)0x05);
        await Assert.That(output[4]).IsEqualTo((byte)0x00);
        await Assert.That(output[5]).IsEqualTo((byte)0x01);
    }

    private static SocksTunnelHandler CreateHandler()
    {
        return new SocksTunnelHandler(NullLogger<SocksTunnelHandler>.Instance, null);
    }
}
