using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A no-op stub implementation of <see cref="IConnectionDispatcher" /> for testing.
///     Records every dispatched connection so tests can verify the listener-to-server
///     callback wiring.
/// </summary>
public sealed class StubConnectionDispatcher : IConnectionDispatcher
{
    /// <summary>
    ///     Gets the list of connections that have been dispatched to this stub.
    /// </summary>
    public List<IProxyConnection> DispatchedConnections { get; } = new();

    /// <inheritdoc />
    public Task DispatchAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        DispatchedConnections.Add(connection);
        return Task.CompletedTask;
    }
}
