using Proxyfan.Domain.Traffic;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Detects whether an HTTP request/response pair represents an RFC 6455 WebSocket upgrade
///     handshake. A valid handshake requires the request to advertise <c>Upgrade: websocket</c>,
///     <c>Connection: upgrade</c> (case-insensitive, multi-token aware), a syntactically valid
///     <c>Sec-WebSocket-Key</c> (16 base64-encoded bytes), and <c>Sec-WebSocket-Version: 13</c>.
///     A successful response must be exactly <c>101 Switching Protocols</c> with a matching
///     <c>Upgrade: websocket</c> header and a <c>Sec-WebSocket-Accept</c> value computed from
///     the request key per RFC 6455 §4.2.2.
/// </summary>
public static class WebSocketUpgradeDetector
{
    /// <summary>
    ///     RFC 6455 §1.3 magic GUID concatenated with the request <c>Sec-WebSocket-Key</c>
    ///     before SHA-1 hashing to derive the expected <c>Sec-WebSocket-Accept</c> value.
    /// </summary>
    private const string WebSocketAcceptGloballyUniqueIdentifier = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

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

        var keyValue = request.Headers.Get("Sec-WebSocket-Key");

        if (!HasValidSecWebSocketKey(keyValue))
        {
            return false;
        }

        var versionValue = request.Headers.Get("Sec-WebSocket-Version");

        if (string.IsNullOrEmpty(versionValue) || !string.Equals(versionValue.Trim(), "13", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when both the request was a WebSocket upgrade
    ///     advertisement and the upstream response confirmed the switch with a 101 status,
    ///     a matching <c>Upgrade: websocket</c> header, and a <c>Sec-WebSocket-Accept</c>
    ///     value derived from the request's <c>Sec-WebSocket-Key</c>.
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

        var responseAccept = response.Headers.Get("Sec-WebSocket-Accept");

        if (string.IsNullOrEmpty(responseAccept))
        {
            return false;
        }

        var requestKey = request.Headers.Get("Sec-WebSocket-Key");
        var expectedAccept = ComputeSecWebSocketAccept(requestKey!);

        if (!string.Equals(responseAccept.Trim(), expectedAccept, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static string ComputeSecWebSocketAccept(string requestKey)
    {
        var concatenated = requestKey.Trim() + WebSocketAcceptGloballyUniqueIdentifier;
        var bytes = Encoding.ASCII.GetBytes(concatenated);
        var hash = SHA1.HashData(bytes);
        return Convert.ToBase64String(hash);
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

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="keyValue" /> is a syntactically
    ///     valid <c>Sec-WebSocket-Key</c> header value — i.e., a base64 encoding of a 16-byte
    ///     nonce as required by RFC 6455 §4.1.
    /// </summary>
    private static bool HasValidSecWebSocketKey(string? keyValue)
    {
        if (string.IsNullOrEmpty(keyValue))
        {
            return false;
        }

        var trimmed = keyValue.Trim();
        Span<byte> decoded = stackalloc byte[16];

        return Convert.TryFromBase64String(trimmed, decoded, out var written) && written == 16;
    }
}
