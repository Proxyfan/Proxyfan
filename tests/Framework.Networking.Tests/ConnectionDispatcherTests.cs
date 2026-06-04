using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain;
using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Proxy.Events;
using Proxyfan.Framework.Networking.Tests.Stubs;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="ConnectionDispatcher" />.
/// </summary>
[NotInParallel]
public sealed class ConnectionDispatcherTests
{
    private static StubConnectionHandler AlwaysMatchHandler()
    {
        return new StubConnectionHandler(_ => true);
    }

    private static StubConnectionHandler AlwaysMatchHandler(ConnectionAcceptedHandler onHandle)
    {
        return new StubConnectionHandler(_ => true, onHandle);
    }

    private static ConnectionDispatcher CreateDispatcher(
        IEnumerable<IConnectionHandler> handlers,
        ProxyOptions? options = null,
        IDomainEventBus? eventBus = null)
    {
        var opts = options ?? new ProxyOptions();
        var monitor = new StubOptionsMonitor<ProxyOptions>(opts);
        var logger = new StubLogger<ConnectionDispatcher>();
        return new ConnectionDispatcher(handlers, monitor, eventBus ?? new StubDomainEventBus(), logger);
    }

    private static StubConnectionHandler MatchesFirstByteHandler(byte expectedByte)
    {
        return new StubConnectionHandler(
            bytes => !bytes.IsEmpty && bytes.FirstSpan[0] == expectedByte);
    }

    private static StubConnectionHandler MatchesFirstByteHandler(byte expectedByte, ConnectionAcceptedHandler onHandle)
    {
        return new StubConnectionHandler(
            bytes => !bytes.IsEmpty && bytes.FirstSpan[0] == expectedByte,
            onHandle);
    }

    private static StubConnectionHandler MatchesPrefixHandler(string asciiPrefix)
    {
        var prefixBytes = Encoding.ASCII.GetBytes(asciiPrefix);
        return new StubConnectionHandler(
            bytes =>
            {
                if (bytes.Length < prefixBytes.Length)
                {
                    return false;
                }

                Span<byte> buffer = stackalloc byte[prefixBytes.Length];
                bytes.Slice(0, prefixBytes.Length).CopyTo(buffer);
                return buffer.SequenceEqual(prefixBytes);
            });
    }

    private static StubConnectionHandler NeverMatchHandler()
    {
        return new StubConnectionHandler(_ => false);
    }

    /// <summary>
    ///     Verifies that when the connection closes with fewer bytes than required,
    ///     the dispatcher still attempts detection with the available bytes and dispatches
    ///     if a handler accepts them.
    /// </summary>
    [Test]
    public async Task DispatchAsync_ConnectionClosedWithPartialBytes_DispatchesToAcceptingHandler()
    {
        var handler = MatchesFirstByteHandler(0x04);
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x04, 0x01, 0x00 });
        await connection.Writer.CompleteAsync();

        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that when the connection is closed before any data arrives,
    ///     the dispatcher returns without invoking any handler.
    /// </summary>
    [Test]
    public async Task DispatchAsync_ConnectionClosedBeforeAnyData_ClosesGracefully()
    {
        var handler = AlwaysMatchHandler();
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.CompleteAsync();
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a handler matching CONNECT bytes is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_ConnectBytes_RoutesToMatchingHandler()
    {
        var handler = MatchesPrefixHandler("CONNECT ");
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1"));
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the first handler in registration order that accepts is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_FirstHandlerRejects_SecondHandlerAccepts()
    {
        var rejecting = NeverMatchHandler();
        var accepting = AlwaysMatchHandler();
        var dispatcher = CreateDispatcher([rejecting, accepting]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(rejecting.HandleCount).IsEqualTo(0);
        await Assert.That(accepting.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that an exception thrown by a handler does not propagate out of
    ///     <see cref="ConnectionDispatcher.DispatchAsync" />.
    /// </summary>
    [Test]
    public async Task DispatchAsync_HandlerThrows_ExceptionDoesNotPropagateToDispatchCaller()
    {
        var handler = AlwaysMatchHandler((_, _) => throw new InvalidOperationException("simulated handler failure"));
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

        await dispatcher.DispatchAsync(connection, CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that after one connection's handler throws, a subsequent connection
    ///     is still dispatched and handled normally by the same dispatcher instance.
    /// </summary>
    [Test]
    public async Task DispatchAsync_HandlerThrowsOnFirstConnection_SubsequentConnectionHandledNormally()
    {
        var callCount = 0;
        var handler = AlwaysMatchHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new InvalidOperationException("simulated failure on first connection");
            }

            return Task.CompletedTask;
        });

        var dispatcher = CreateDispatcher([handler]);

        var failingConnection = new StubProxyConnection();
        await failingConnection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        await dispatcher.DispatchAsync(failingConnection, CancellationToken.None);

        var secondConnection = new StubProxyConnection();
        await secondConnection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        await dispatcher.DispatchAsync(secondConnection, CancellationToken.None);

        await Assert.That(callCount).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that when a handler throws, a <see cref="ConnectionErrorOccurred" /> event is
    ///     published with the correct client endpoint.
    /// </summary>
    [Test]
    public async Task DispatchAsync_HandlerThrows_PublishesConnectionErrorOccurredEvent()
    {
        var bus = new StubDomainEventBus();
        var handler = AlwaysMatchHandler((_, _) => throw new InvalidOperationException("simulated handler failure"));
        var dispatcher = CreateDispatcher([handler], eventBus: bus);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        var events = new List<ConnectionErrorOccurred>(bus.PublishedOf<ConnectionErrorOccurred>());
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].RemoteEndPoint).IsSameReferenceAs(connection.RemoteEndPoint);
        await Assert.That(events[0].Error).IsNotNull();
        await Assert.That(events[0].Error).IsTypeOf<ConnectionHandlerError>();
        var handlerError = (ConnectionHandlerError)events[0].Error;
        await Assert.That(handlerError.Code).IsEqualTo("CONNECTION_HANDLER_FAULTED");
        await Assert.That(handlerError.ExceptionTypeName).IsEqualTo(typeof(InvalidOperationException).FullName);
        await Assert.That(handlerError.InnerException).IsNull();
        await Assert.That(handlerError.Message).DoesNotContain("simulated handler failure");
    }

    /// <summary>
    ///     Verifies that an <see cref="OperationCanceledException" /> thrown by a handler
    ///     because the outer cancellation token was cancelled propagates out of the dispatcher.
    /// </summary>
    [Test]
    public async Task DispatchAsync_HandlerThrowsOperationCanceledException_PropagatesOutOfDispatcher()
    {
        using var cts = new CancellationTokenSource();

        var handler = AlwaysMatchHandler(async (_, ct) =>
        {
            await cts.CancelAsync();
            ct.ThrowIfCancellationRequested();
        });

        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

        await Assert.That(
            async () => await dispatcher.DispatchAsync(connection, cts.Token)
        ).Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that a handler matching HTTP GET bytes is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_HttpGetBytes_RoutesToMatchingHandler()
    {
        var handler = MatchesPrefixHandler("GET / HT");
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTT"));
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that with an empty handler collection the dispatcher closes gracefully.
    /// </summary>
    [Test]
    public async Task DispatchAsync_NoHandlersRegistered_ClosesGracefully()
    {
        var dispatcher = CreateDispatcher([]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTT"));

        await dispatcher.DispatchAsync(connection, CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that when the outer cancellation token is cancelled during detection,
    ///     an <see cref="OperationCanceledException" /> propagates to the caller.
    /// </summary>
    [Test]
    public async Task DispatchAsync_OuterCancellationDuringDetection_ThrowsOperationCanceledException()
    {
        var handler = AlwaysMatchHandler();
        var options = new ProxyOptions { ProtocolDetectionTimeout = TimeSpan.Zero };
        var dispatcher = CreateDispatcher([handler], options);
        var connection = new StubProxyConnection();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.That(
            async () => await dispatcher.DispatchAsync(connection, cts.Token)
        ).Throws<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that when data arrives in two partial writes (slow client),
    ///     the dispatcher waits and correctly routes once enough bytes are available.
    /// </summary>
    [Test]
    public async Task DispatchAsync_DataArrivesInChunks_RoutesWithoutDelay()
    {
        var dispatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = MatchesFirstByteHandler(0x04);
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        var writeTask = Task.Run(async () =>
        {
            await connection.Writer.WriteAsync(new byte[] { 0x04, 0x01 });
            await dispatchStarted.Task;
            await connection.Writer.WriteAsync(new byte[] { 0x00, 0x50, 0x7F, 0x00, 0x00, 0x01 });
        });

        var dispatchTask = dispatcher.DispatchAsync(connection, CancellationToken.None);
        dispatchStarted.SetResult();
        await dispatchTask;
        await writeTask;

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that the handler receives the original initial bytes in its transport pipe,
    ///     even though the dispatcher already read them during protocol detection.
    /// </summary>
    [Test]
    public async Task DispatchAsync_WhenHandlerMatches_ReceivesAllPeekedBytes()
    {
        var expectedBytes = Encoding.ASCII.GetBytes("GET / HT");
        byte[]? capturedBytes = null;

        var handler = AlwaysMatchHandler(async (conn, ct) =>
        {
            var result = await conn.Transport.Input.ReadAsync(ct);
            capturedBytes = result.Buffer.ToArray();
            conn.Transport.Input.AdvanceTo(result.Buffer.End);
        });

        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(expectedBytes);
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(capturedBytes).IsNotNull();
        await Assert.That(capturedBytes!.Length).IsEqualTo(expectedBytes.Length);

        for (var i = 0; i < expectedBytes.Length; i++)
        {
            await Assert.That(capturedBytes[i]).IsEqualTo(expectedBytes[i]);
        }
    }

    /// <summary>
    ///     Verifies that a handler matching SOCKS4 first byte (0x04) is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_Socks4Bytes_RoutesToMatchingHandler()
    {
        var handler = MatchesFirstByteHandler(0x04);
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x04, 0x01, 0x00, 0x50, 0x7F, 0x00, 0x00, 0x01 });
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that a handler matching SOCKS5 first byte (0x05) is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_Socks5Bytes_RoutesToMatchingHandler()
    {
        var handler = MatchesFirstByteHandler(0x05);
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that when protocol detection times out before enough data arrives,
    ///     the dispatcher returns without invoking any handler.
    /// </summary>
    [Test]
    public async Task DispatchAsync_TimeoutBeforeEnoughData_ClosesGracefully()
    {
        var handler = AlwaysMatchHandler();
        var options = new ProxyOptions { ProtocolDetectionTimeout = TimeSpan.FromMilliseconds(50) };
        var dispatcher = CreateDispatcher([handler], options);
        var connection = new StubProxyConnection();

        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that when no handler accepts the bytes, no handler's HandleAsync is invoked.
    /// </summary>
    [Test]
    public async Task DispatchAsync_UnknownProtocol_NoHandlerInvoked()
    {
        var handler = NeverMatchHandler();
        var dispatcher = CreateDispatcher([handler]);
        var connection = new StubProxyConnection();

        await connection.Writer.WriteAsync(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
        await dispatcher.DispatchAsync(connection, CancellationToken.None);

        await Assert.That(handler.HandleCount).IsEqualTo(0);
    }
}