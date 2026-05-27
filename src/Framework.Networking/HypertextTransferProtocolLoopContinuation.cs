using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure decision helper used by
///     <see cref="TransportLayerSecurityInterceptorHandler" /> to decide whether the
///     intercepted HTTP exchange loop should continue or terminate after handling a single
///     request/response. Connection-close indicators (HTTP/1.0 default, Connection: close,
///     or an empty/aborted request line) end the loop; everything else keeps it alive for
///     keep-alive pipelining.
/// </summary>
public static class HypertextTransferProtocolLoopContinuation
{
    /// <summary>
    ///     Returns true when the loop should continue to accept another request on the same
    ///     connection.
    /// </summary>
    /// <param name="hypertextTransferProtocolVersion">
    ///     The HTTP version of the most recently completed request (e.g. "HTTP/1.1").
    /// </param>
    /// <param name="connectionHeaderValue">
    ///     The value of the Connection request header, or null when absent.
    /// </param>
    /// <param name="hadAbortedRequest">
    ///     True when the last request could not be parsed and the connection was reset.
    /// </param>
    /// <returns>True to continue; false to terminate the loop.</returns>
    public static bool CanContinue(string? hypertextTransferProtocolVersion, string? connectionHeaderValue, bool hadAbortedRequest)
    {
        if (hadAbortedRequest)
        {
            return false;
        }

        if (string.Equals(connectionHeaderValue, "close", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var version = hypertextTransferProtocolVersion ?? string.Empty;

        if (version.Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(connectionHeaderValue, "keep-alive", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
