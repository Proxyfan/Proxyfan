using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure decision helper used by
///     <see cref="TransportLayerSecurityInterceptorHandler" /> to decide whether the
///     intercepted HTTP exchange loop should continue or terminate after handling a single
///     request/response. Connection-close indicators (HTTP/1.0 default, a "close" token in
///     the Connection header, or an empty/aborted request line) end the loop; everything
///     else keeps it alive for keep-alive pipelining. The Connection header is parsed as a
///     comma-separated token list per RFC 7230.
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

        if (HasConnectionToken(connectionHeaderValue, "close"))
        {
            return false;
        }

        var version = hypertextTransferProtocolVersion ?? string.Empty;

        if (version.Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
        {
            return HasConnectionToken(connectionHeaderValue, "keep-alive");
        }

        return true;
    }

    private static bool HasConnectionToken(string? connectionHeaderValue, string token)
    {
        if (string.IsNullOrEmpty(connectionHeaderValue))
        {
            return false;
        }

        var tokens = connectionHeaderValue.Split(',');
        foreach (var candidate in tokens)
        {
            if (candidate.AsSpan().Trim().Equals(token.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
