using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Proxyfan.Domain.Proxy;

namespace Proxyfan.Framework.Networking.Tests.Stubs;

/// <summary>
///     A configurable stub implementation of <see cref="IConnectionHandler" /> for testing.
/// </summary>
public sealed class StubConnectionHandler : IConnectionHandler
{
    private readonly Func<ReadOnlySequence<byte>, bool> _canHandle;
    private readonly ConnectionAcceptedHandler? _onHandle;

    /// <summary>
    ///     Gets the total number of times <see cref="HandleAsync" /> was invoked.
    /// </summary>
    public int HandleCount { get; private set; }

    /// <summary>
    ///     Initializes a new <see cref="StubConnectionHandler" /> that always performs a no-op when handling.
    /// </summary>
    /// <param name="canHandle">
    ///     The delegate invoked by <see cref="CanHandle" /> to determine if this handler accepts the connection.
    /// </param>
    public StubConnectionHandler(Func<ReadOnlySequence<byte>, bool> canHandle)
    {
        _canHandle = canHandle;
        _onHandle = null;
    }

    /// <summary>
    ///     Initializes a new <see cref="StubConnectionHandler" /> with a custom handle callback.
    /// </summary>
    /// <param name="canHandle">
    ///     The delegate invoked by <see cref="CanHandle" /> to determine if this handler accepts the connection.
    /// </param>
    /// <param name="onHandle">
    ///     The delegate invoked by <see cref="HandleAsync" /> to process the connection.
    /// </param>
    public StubConnectionHandler(Func<ReadOnlySequence<byte>, bool> canHandle, ConnectionAcceptedHandler onHandle)
    {
        _canHandle = canHandle;
        _onHandle = onHandle;
    }

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