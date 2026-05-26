using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Defines operations for registering and unregistering the system proxy settings.
/// </summary>
public interface ISystemProxy
{
    /// <summary>
    ///     Registers the application as the system proxy for the specified port.
    /// </summary>
    /// <param name="port">The local listening port.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when registration has finished.</returns>
    Task RegisterAsync(int port, CancellationToken cancellationToken);

    /// <summary>
    ///     Removes the application from the system proxy settings.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when unregistration has finished.</returns>
    Task UnregisterAsync(CancellationToken cancellationToken);
}