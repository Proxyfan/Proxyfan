using Proxyfan.Domain.Traffic;
using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Detects whether an HTTP request/response pair represents an RFC 6455 WebSocket upgrade
///     handshake. A valid handshake requires the request to advertise <c>Upgrade: websocket</c>
///     plus <c>Connection: upgrade</c> (case-insensitive, multi-token aware), and the response
///     status to be exactly <c>101 Switching Protocols</c> with matching <c>Upgrade</c> header.
/// </summary>
public static class WebSocketUpgradeDetector
{
    /// <summary>
    ///     Returns <see langword="true" /> when the request contains a WebSocket upgrade
    ///     advertisement (used to know whether to await a 101 response and switch to tunnel
    ///     mode).
    /// </summary>
    /// <param name="request">The parsed HTTP request.</param>
    /// <returns><see langword="true" /> when the request is a WebSocket upgrade attempt.</returns>
    public static bool HasWebSocketUpgradeRequest(HypertextTransferProtocolRequestData request)
    {
        var upgradeValue = request.Headers.Get("Upgrade");

        if (string.IsNullOrEmpty(upgradeValue) || !HasToken(upgradeValue, "websocket"))
        {
            return false;
        }

        var connectionValue = request.Headers.Get("Connection");

        if (string.IsNullOrEmpty(connectionValue) || !HasToken(connectionValue, "upgrade"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when both the request was a WebSocket upgrade
    ///     advertisement and the upstream response confirmed the switch with a 101 status
    ///     and matching <c>Upgrade: websocket</c> header.
    /// </summary>
    /// <param name="request">The original HTTP request.</param>
    /// <param name="response">The upstream response.</param>
    /// <returns><see langword="true" /> when a WebSocket tunnel should be established.</returns>
    public static bool HasWebSocketUpgradeSuccess(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        if (!HasWebSocketUpgradeRequest(request))
        {
            return false;
        }

        if (response.StatusCode != 101)
        {
            return false;
        }

        var responseUpgrade = response.Headers.Get("Upgrade");

        if (string.IsNullOrEmpty(responseUpgrade) || !HasToken(responseUpgrade, "websocket"))
        {
            return false;
        }

        return true;
    }

    private static bool HasToken(string headerValue, string token)
    {
        var parts = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (string.Equals(part, token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
