using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy.Tests.Stubs;

/// <summary>
///     A no-op stub implementation of <see cref="IConnectionDispatcher" /> for testing.
/// </summary>
public sealed class StubConnectionDispatcher : IConnectionDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}