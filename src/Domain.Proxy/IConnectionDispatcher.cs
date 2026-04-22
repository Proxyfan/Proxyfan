using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Defines the contract for a component that accepts an incoming connection,
///     detects its protocol, and routes it to the appropriate handler.
/// </summary>
public interface IConnectionDispatcher
{
    /// <summary>
    ///     Detects the protocol of <paramref name="connection" /> from its first bytes and
    ///     dispatches to the appropriate registered handler.
    /// </summary>
    /// <param name="connection">The accepted connection to inspect and dispatch.</param>
    /// <param name="cancellationToken">A token that, when cancelled, stops the dispatch.</param>
    /// <returns>A <see cref="Task" /> that completes when the connection has been fully handled.</returns>
    Task DispatchAsync(IProxyConnection connection, CancellationToken cancellationToken);
}
