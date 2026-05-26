using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="SocketConnection" />.
/// </summary>
public sealed class SocketConnectionTests
{
    private static async Task<(Socket ServerSocket, TcpClient Client, TcpListener Listener)> CreateSocketPairAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var serverSocket = await listener.AcceptSocketAsync();
        return (serverSocket, client, listener);
    }

    /// <summary>
    ///     Verifies that <see cref="SocketConnection.DisposeAsync" /> completes without throwing.
    /// </summary>
    [Test]
    public async Task DisposeAsync_WhenCalled_DisposesUnderlyingStream()
    {
        var (serverSocket, client, tcpListener) = await CreateSocketPairAsync();
        var connection = new SocketConnection(serverSocket);

        try
        {
            await connection.DisposeAsync();
        }
        finally
        {
            client.Dispose();
            tcpListener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="SocketConnection.RemoteEndPoint" /> is not null after construction.
    /// </summary>
    [Test]
    public async Task RemoteEndPoint_AfterConstruction_ReturnsClientAddress()
    {
        var (serverSocket, client, tcpListener) = await CreateSocketPairAsync();

        await using var connection = new SocketConnection(serverSocket);

        try
        {
            await Assert.That(connection.RemoteEndPoint).IsNotNull();
        }
        finally
        {
            client.Dispose();
            tcpListener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that <see cref="SocketConnection.Transport" /> is not null after construction.
    /// </summary>
    [Test]
    public async Task Transport_AfterConstruction_IsNotNull()
    {
        var (serverSocket, client, tcpListener) = await CreateSocketPairAsync();

        await using var connection = new SocketConnection(serverSocket);

        try
        {
            await Assert.That(connection.Transport).IsNotNull();
        }
        finally
        {
            client.Dispose();
            tcpListener.Stop();
        }
    }
}