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
///     Tests for <see cref="BidirectionalStreamPump" /> using real loopback TCP sockets so
///     two independent pairs of network streams form the client and backend ends.
/// </summary>
[NotInParallel]
public sealed class BidirectionalStreamPumpTests
{
    /// <summary>
    ///     Verifies bytes written to the client appear at the backend and vice versa when
    ///     the pump is bridging the two pairs.
    /// </summary>
    [Test]
    public async Task PumpAsync_ConnectedSockets_RelaysBothDirections()
    {
        var clientPair = await CreateConnectedPairAsync();
        var backendPair = await CreateConnectedPairAsync();

        using var clientSocket = clientPair.Outer;
        using var proxyClientSide = clientPair.Inner;
        using var proxyBackendSide = backendPair.Outer;
        using var backendSocket = backendPair.Inner;

        using var clientStream = new NetworkStream(clientSocket, ownsSocket: false);
        using var proxyClientStream = new NetworkStream(proxyClientSide, ownsSocket: false);
        using var proxyBackendStream = new NetworkStream(proxyBackendSide, ownsSocket: false);
        using var backendStream = new NetworkStream(backendSocket, ownsSocket: false);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var pumpTask = BidirectionalStreamPump.PumpAsync(proxyClientStream, proxyBackendStream, bufferSize: 1024, cts.Token);

        var fromClient = Encoding.ASCII.GetBytes("c2b");
        await clientStream.WriteAsync(fromClient, cts.Token);
        var receivedAtBackend = new byte[3];
        var readAtBackend = await ReadFullyAsync(backendStream, receivedAtBackend, cts.Token);

        var fromBackend = Encoding.ASCII.GetBytes("b2c");
        await backendStream.WriteAsync(fromBackend, cts.Token);
        var receivedAtClient = new byte[3];
        var readAtClient = await ReadFullyAsync(clientStream, receivedAtClient, cts.Token);

        await Assert.That(readAtBackend).IsEqualTo(3);
        await Assert.That(Encoding.ASCII.GetString(receivedAtBackend)).IsEqualTo("c2b");
        await Assert.That(readAtClient).IsEqualTo(3);
        await Assert.That(Encoding.ASCII.GetString(receivedAtClient)).IsEqualTo("b2c");

        clientSocket.Shutdown(SocketShutdown.Send);
        try
        {
            await pumpTask;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
        }
    }

    /// <summary>
///     Verifies the pump completes when the source stream returns EOF (zero-byte read).
/// </summary>
    [Test]
    public async Task PumpAsync_SourceReturnsEndOfFile_PumpCompletes()
    {
        var leftBuffer = new MemoryStream();
        var rightBuffer = new MemoryStream();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await BidirectionalStreamPump.PumpAsync(leftBuffer, rightBuffer, bufferSize: 1024, cts.Token);

        await Assert.That(rightBuffer.Length).IsEqualTo(0L);
    }

    /// <summary>
    ///     Verifies that an IOException raised during a read terminates that pump direction
    ///     gracefully without propagating the exception.
    /// </summary>
    [Test]
    public async Task PumpAsync_SourceReadThrowsIoException_TerminatesDirectionWithoutThrowing()
    {
        var faultedLeft = new ThrowingStream(throwOnRead: new IOException("simulated"));
        var rightBuffer = new MemoryStream();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await BidirectionalStreamPump.PumpAsync(faultedLeft, rightBuffer, bufferSize: 1024, cts.Token);
    }

    /// <summary>
    ///     Verifies that a SocketException raised during a read terminates that direction.
    /// </summary>
    [Test]
    public async Task PumpAsync_SourceReadThrowsSocketException_TerminatesDirectionWithoutThrowing()
    {
        var faultedLeft = new ThrowingStream(throwOnRead: new SocketException());
        var rightBuffer = new MemoryStream();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await BidirectionalStreamPump.PumpAsync(faultedLeft, rightBuffer, bufferSize: 1024, cts.Token);
    }

    /// <summary>
    ///     Verifies that an IOException raised during a write terminates that direction.
    /// </summary>
    [Test]
    public async Task PumpAsync_DestinationWriteThrowsIoException_TerminatesDirectionWithoutThrowing()
    {
        var sourceLeft = new MemoryStream(Encoding.ASCII.GetBytes("payload"));
        var faultedRight = new ThrowingStream(throwOnWrite: new IOException("simulated"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await BidirectionalStreamPump.PumpAsync(sourceLeft, faultedRight, bufferSize: 1024, cts.Token);
    }

    /// <summary>
    ///     Verifies that a SocketException raised during a write terminates that direction.
    /// </summary>
    [Test]
    public async Task PumpAsync_DestinationWriteThrowsSocketException_TerminatesDirectionWithoutThrowing()
    {
        var sourceLeft = new MemoryStream(Encoding.ASCII.GetBytes("payload"));
        var faultedRight = new ThrowingStream(throwOnWrite: new SocketException());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await BidirectionalStreamPump.PumpAsync(sourceLeft, faultedRight, bufferSize: 1024, cts.Token);
    }

    private static async Task<(Socket Outer, Socket Inner)> CreateConnectedPairAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var connectTask = Task.Run(async () =>
            {
                var outer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await outer.ConnectAsync(IPAddress.Loopback, port);
                return outer;
            });
            var inner = await listener.AcceptSocketAsync();
            var outer = await connectTask;
            return (outer, inner);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
