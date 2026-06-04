using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Represents the asynchronous operation that accepts a new inbound <see cref="Socket" />
///     from a bound listen socket. Injected into <see cref="SocketProxyListener" /> via
///     <see cref="SocketProxyListener.AcceptOverride" /> to allow accept-loop behaviour to be
///     exercised under test without requiring a live network stack.
/// </summary>
/// <param name="listenSocket">
///     The bound, listening <see cref="Socket" /> on which to accept incoming connections.
/// </param>
/// <param name="cancellationToken">
///     A token whose cancellation should abort the accept operation.
/// </param>
/// <returns>
///     A <see cref="ValueTask{TResult}" /> that resolves to the newly accepted
///     <see cref="Socket" />.
/// </returns>
public delegate ValueTask<Socket> SocketAcceptDelegate(Socket listenSocket, CancellationToken cancellationToken);
