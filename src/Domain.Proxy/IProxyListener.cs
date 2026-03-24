using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Defines the lifecycle contract for a TCP proxy listener that binds to a port,
///     accepts incoming connections, and dispatches each connection via a callback.
/// </summary>
public interface IProxyListener
{
    /// <summary>Gets a value indicating whether the listener is currently bound and accepting connections.</summary>
    bool IsListening { get; }

    /// <summary>
    ///     Gets the port number the listener is actually bound to, or <see langword="null" />
    ///     if the listener is not currently active.
    /// </summary>
    int? BoundPort { get; }

    /// <summary>
    ///     Starts the listener, binds to the configured port, and begins accepting incoming connections.
    ///     Each accepted connection is delivered to <paramref name="onConnectionAccepted" />.
    /// </summary>
    /// <param name="onConnectionAccepted">
    ///     An asynchronous callback invoked for each accepted connection.
    ///     The connection is passed as the first argument; the cancellation token reflects the listener lifetime.
    /// </param>
    /// <param name="cancellationToken">A token that, when cancelled, stops the listener gracefully.</param>
    /// <returns>A <see cref="Task" /> that completes when the listener has bound and the accept loop is running.</returns>
    /// <exception cref="ProxyBindException">Thrown when the configured port is already in use or access is denied.</exception>
    Task StartAsync(Func<IProxyConnection, CancellationToken, Task> onConnectionAccepted, CancellationToken cancellationToken);

    /// <summary>
    ///     Stops the listener gracefully, allowing in-flight connection handling to complete
    ///     within the scope of <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="cancellationToken">A token that forces an immediate stop if cancelled.</param>
    /// <returns>A <see cref="Task" /> that completes when the listener has fully stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
