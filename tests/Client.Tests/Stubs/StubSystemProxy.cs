using Proxyfan.Domain.Proxy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     A stub implementation of <see cref="ISystemProxy" /> that records calls
///     for assertion in unit tests.
/// </summary>
internal sealed class StubSystemProxy : ISystemProxy
{
    private readonly List<int> _registeredPorts;
    private int _unregisterCount;

    /// <summary>
    ///     Gets the ports that have been registered, in call order.
    /// </summary>
    public IReadOnlyList<int> RegisteredPorts => _registeredPorts;

    /// <summary>
    ///     Gets the number of times <see cref="UnregisterAsync" /> was called.
    /// </summary>
    public int UnregisterCount => _unregisterCount;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubSystemProxy" />.
    /// </summary>
    public StubSystemProxy()
    {
        var registeredPorts = new List<int>();
        _registeredPorts = registeredPorts;
        _unregisterCount = 0;
    }

    /// <inheritdoc />
    public Task RegisterAsync(int port, CancellationToken cancellationToken)
    {
        _registeredPorts.Add(port);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(CancellationToken cancellationToken)
    {
        _unregisterCount++;
        return Task.CompletedTask;
    }
}