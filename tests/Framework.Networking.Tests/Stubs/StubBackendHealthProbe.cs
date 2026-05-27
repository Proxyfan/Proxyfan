using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A configurable stub <see cref="IBackendHealthProbe" /> that returns the supplied
///     health value and records the probes it received.
/// </summary>
public sealed class StubBackendHealthProbe : IBackendHealthProbe
{
    /// <summary>
    ///     Gets or sets the response the probe returns.
    /// </summary>
    public bool ResponseHealthy { get; set; }

    /// <summary>
    ///     Gets the number of probes received.
    /// </summary>
    public int ProbeCount { get; private set; }

    /// <inheritdoc />
    public Task<bool> ProbeAsync(string host, int port, CancellationToken cancellationToken)
    {
        _ = (host, port, cancellationToken);
        ProbeCount++;
        return Task.FromResult(ResponseHealthy);
    }
}
