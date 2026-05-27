using Proxyfan.Domain.Proxy;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     TCP-based <see cref="IBackendHealthProbe" />. Attempts to open a short-lived TCP
///     connection to the backend; success indicates the backend is reachable.
/// </summary>
public sealed class TransportControlProtocolBackendHealthProbe : IBackendHealthProbe
{
    /// <inheritdoc />
    public async Task<bool> ProbeAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            return client.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (System.OperationCanceledException)
        {
            return false;
        }
    }
}
