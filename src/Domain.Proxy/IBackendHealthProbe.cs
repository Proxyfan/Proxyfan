using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Contract for a backend health probe. The reverse proxy engine periodically calls
///     <see cref="ProbeAsync" /> to determine whether a backend is reachable. Implementations
///     typically open a short-lived TCP connection or HTTP HEAD request to the target.
/// </summary>
public interface IBackendHealthProbe
{
    /// <summary>
    ///     Attempts to reach <paramref name="host" />:<paramref name="port" /> and returns whether
    ///     it appears healthy.
    /// </summary>
    /// <param name="host">The backend host to probe.</param>
    /// <param name="port">The backend TCP port to probe.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns><see langword="true" /> when the backend is reachable.</returns>
    Task<bool> ProbeAsync(string host, int port, CancellationToken cancellationToken);
}
