using System.Net.Sockets;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure decision helper used by <see cref="SocketProxyListener" /> to determine whether
///     a <see cref="SocketException" /> raised during <c>AcceptAsync</c> should cause the
///     accept loop to terminate or simply retry.
/// </summary>
public static class AcceptErrorClassifier
{
    /// <summary>
    ///     Returns true when the accept loop should terminate (because the listener has
    ///     been cancelled or the socket has been closed) rather than continue.
    /// </summary>
    /// <param name="exception">The accept-time exception.</param>
    /// <param name="cancellationRequested">
    ///     Whether the caller's cancellation token has been cancelled.
    /// </param>
    /// <returns>True to terminate the loop; false to continue accepting.</returns>
    public static bool HasFatalError(SocketException exception, bool cancellationRequested)
    {
        if (cancellationRequested)
        {
            return true;
        }

        var code = exception.SocketErrorCode;
        return code is SocketError.OperationAborted or SocketError.Interrupted;
    }
}
