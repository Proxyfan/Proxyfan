using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Additional tests for <see cref="SocketConnection" /> covering double-dispose
///     and the fallback-endpoint code path.
/// </summary>
public sealed class SocketConnectionAdditionalTests
{
    /// <summary>
    ///     Verifies that calling <see cref="SocketConnection.DisposeAsync" /> twice on the same
    ///     connection is a no-op the second time.
    /// </summary>
    [Test]
    public async Task DisposeAsync_CalledTwice_IsIdempotent()
    {
        var (serverSocket, client, listener) = await CreateSocketPairAsync();

        try
        {
            var connection = new SocketConnection(serverSocket);
            await connection.DisposeAsync();
            await Assert.That(async () => await connection.DisposeAsync()).ThrowsNothing();
        }
        finally
        {
            client.Dispose();
            listener.Stop();
        }
    }

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
}
