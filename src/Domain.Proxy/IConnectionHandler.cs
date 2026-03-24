using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Defines the contract for a component that handles an incoming proxy connection
///     for a specific protocol.
/// </summary>
/// <remarks>
///     Implementations are registered with a <c>ConnectionDispatcher</c>, which reads the
///     first bytes of each accepted connection, calls <see cref="CanHandle" /> on each
///     registered handler in order, and delegates to the first handler that returns
///     <see langword="true" />.
/// </remarks>
public interface IConnectionHandler
{
    /// <summary>
    ///     Determines whether this handler can process a connection whose opening bytes
    ///     match the protocol this handler is responsible for.
    /// </summary>
    /// <param name="initialBytes">
    ///     The first bytes received on the connection (up to 8 bytes).
    ///     May contain fewer bytes if the client closed the connection early.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if this handler recognises the protocol in
    ///     <paramref name="initialBytes" />; <see langword="false" /> if the bytes are
    ///     insufficient or do not match this handler's protocol.
    /// </returns>
    bool CanHandle(ReadOnlySequence<byte> initialBytes);

    /// <summary>
    ///     Handles the connection. The connection's transport pipe contains all original
    ///     bytes including those peeked during protocol detection, so implementations
    ///     may read from the start of the stream without special handling.
    /// </summary>
    /// <param name="connection">The accepted connection to handle.</param>
    /// <param name="cancellationToken">A token that, when cancelled, requests graceful termination.</param>
    /// <returns>A <see cref="Task" /> that completes when the connection has been fully handled.</returns>
    Task HandleAsync(IProxyConnection connection, CancellationToken cancellationToken);
}
