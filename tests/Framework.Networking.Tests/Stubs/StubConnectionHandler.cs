using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A configurable stub implementation of <see cref="IConnectionHandler" /> for testing.
/// </summary>
internal sealed class StubConnectionHandler : IConnectionHandler
{
    private readonly Func<ReadOnlySequence<byte>, bool> _canHandle;
    private readonly Func<IProxyConnection, CancellationToken, Task>? _onHandle;

    /// <summary>
    ///     Initializes a new instance of <see cref="StubConnectionHandler" />.
    /// </summary>
    /// <param name="canHandle">The delegate invoked by <see cref="CanHandle" />.</param>
    /// <param name="onHandle">Optional delegate invoked by <see cref="HandleAsync" />. Defaults to a no-op.</param>
    internal StubConnectionHandler(
        Func<ReadOnlySequence<byte>, bool> canHandle,
        Func<IProxyConnection, CancellationToken, Task>? onHandle = null)
    {
        _canHandle = canHandle;
        _onHandle = onHandle;
    }

    /// <summary>Gets the number of times <see cref="HandleAsync" /> was invoked.</summary>
    internal int HandleCount { get; private set; }

    /// <inheritdoc />
    public bool CanHandle(ReadOnlySequence<byte> initialBytes)
    {
        return _canHandle(initialBytes);
    }

    /// <inheritdoc />
    public async Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken)
    {
        HandleCount++;

        if (_onHandle is not null)
        {
            await _onHandle(connection, cancellationToken).ConfigureAwait(false);
        }
    }
}
