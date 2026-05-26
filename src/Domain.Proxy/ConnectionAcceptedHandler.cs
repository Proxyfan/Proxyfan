using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Represents the asynchronous callback invoked by <see cref="IProxyListener" /> each time a
///     new incoming client connection is accepted and ready for protocol detection and dispatching.
/// </summary>
/// <param name="connection">
///     The accepted, fully connected proxy client.
/// </param>
/// <param name="cancellationToken">
///     A token that reflects the listener lifetime and is cancelled when the listener stops.
/// </param>
/// <returns>
///     A <see cref="Task" /> that completes when all handling for this connection finishes.
/// </returns>
public delegate Task ConnectionAcceptedHandler(IProxyConnection connection, CancellationToken cancellationToken);