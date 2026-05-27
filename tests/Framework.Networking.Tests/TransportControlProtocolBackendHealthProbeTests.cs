using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Tests for <see cref="TransportControlProtocolBackendHealthProbe" />.
/// </summary>
[NotInParallel]
public sealed class TransportControlProtocolBackendHealthProbeTests
{
    /// <summary>
    ///     Verifies that probing a listening port reports healthy.
    /// </summary>
    [Test]
    public async Task ProbeAsync_ListeningBackend_ReturnsTrue()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var probe = new TransportControlProtocolBackendHealthProbe();

            var healthy = await probe.ProbeAsync("127.0.0.1", port, CancellationToken.None);

            await Assert.That(healthy).IsTrue();
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    ///     Verifies that probing an unbound port reports unhealthy.
    /// </summary>
    [Test]
    public async Task ProbeAsync_NoBackend_ReturnsFalse()
    {
        var probe = new TransportControlProtocolBackendHealthProbe();
        var unboundPort = GetFreePort();

        var healthy = await probe.ProbeAsync("127.0.0.1", unboundPort, CancellationToken.None);

        await Assert.That(healthy).IsFalse();
    }

    /// <summary>
    ///     Verifies that a cancelled probe returns false rather than throwing.
    /// </summary>
    [Test]
    public async Task ProbeAsync_Cancelled_ReturnsFalse()
    {
        var probe = new TransportControlProtocolBackendHealthProbe();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var healthy = await probe.ProbeAsync("10.255.255.1", 1, cts.Token);

        await Assert.That(healthy).IsFalse();
    }

    private static int GetFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
