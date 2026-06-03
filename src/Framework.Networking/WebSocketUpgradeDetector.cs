using Proxyfan.Domain.Traffic;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Detects whether an HTTP request/response pair represents an RFC 6455 WebSocket upgrade
///     handshake. A valid handshake requires an HTTP/1.1 <c>GET</c> request that advertises
///     <c>Upgrade: websocket</c> plus <c>Connection: upgrade</c> (case-insensitive, multi-token
///     aware), a syntactically valid <c>Sec-WebSocket-Key</c> (16 random bytes, base64-encoded),
///     and <c>Sec-WebSocket-Version: 13</c>. The response must be exactly <c>101 Switching Protocols</c>
///     with matching <c>Upgrade: websocket</c> header and a <c>Sec-WebSocket-Accept</c> value that
///     matches the SHA-1+base64 transform of the request key concatenated with the RFC 6455 GUID.
/// </summary>
public static class WebSocketUpgradeDetector
{
    /// <summary>
    ///     RFC 6455 §4.2.2 magic GUID concatenated with the request <c>Sec-WebSocket-Key</c>
    ///     to compute the expected <c>Sec-WebSocket-Accept</c> response value.
    /// </summary>
    private const string WebSocketAcceptGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    /// <summary>
    ///     Returns <see langword="true" /> when the request contains a WebSocket upgrade
    ///     advertisement (used to know whether to await a 101 response and switch to tunnel
    ///     mode).
    /// </summary>
    /// <param name="request">The parsed HTTP request.</param>
    /// <returns><see langword="true" /> when the request is a WebSocket upgrade attempt.</returns>
    public static bool HasWebSocketUpgradeRequest(HypertextTransferProtocolRequestData request)
    {
        if (!string.Equals(request.Method, "GET", StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(request.Version, "HTTP/1.1", StringComparison.Ordinal))
        {
            return false;
        }

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

        var versionValue = request.Headers.Get("Sec-WebSocket-Version");

        if (string.IsNullOrEmpty(versionValue) || !string.Equals(versionValue.Trim(), "13", StringComparison.Ordinal))
        {
            return false;
        }

        var keyValue = request.Headers.Get("Sec-WebSocket-Key");

        if (string.IsNullOrEmpty(keyValue) || !HasValidWebSocketKey(keyValue.Trim()))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when both the request was a WebSocket upgrade
    ///     advertisement and the upstream response confirmed the switch with a 101 status,
    ///     matching <c>Upgrade: websocket</c> header, and a <c>Sec-WebSocket-Accept</c> value
    ///     that matches the SHA-1+base64 transform of the request key per RFC 6455 §4.2.2.
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

        var requestKey = request.Headers.Get("Sec-WebSocket-Key")?.Trim();
        var responseAccept = response.Headers.Get("Sec-WebSocket-Accept")?.Trim();

        if (string.IsNullOrEmpty(requestKey) || string.IsNullOrEmpty(responseAccept))
        {
            return false;
        }

        var expectedAccept = ComputeExpectedAccept(requestKey);

        if (!string.Equals(responseAccept, expectedAccept, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     RFC 6455 §4.2.2: <c>Sec-WebSocket-Accept</c> is the base64 encoding of the SHA-1 hash
    ///     of the request key concatenated with the well-known GUID. SHA-1 is mandated by the
    ///     protocol and is used here only as a handshake integrity check, not for cryptographic
    ///     secrecy.
    /// </summary>
    private static string ComputeExpectedAccept(string requestKey)
    {
        var concatenated = Encoding.ASCII.GetBytes(requestKey + WebSocketAcceptGuid);
        var hash = SHA1.HashData(concatenated);
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
    ///     RFC 6455 §4.1: a valid <c>Sec-WebSocket-Key</c> is the base64 encoding of a 16-byte
    ///     random nonce. Reject anything that fails to decode or whose decoded length differs.
    /// </summary>
    private static bool HasValidWebSocketKey(string keyValue)
    {
        Span<byte> buffer = stackalloc byte[32];

        if (!Convert.TryFromBase64String(keyValue, buffer, out var written))
        {
            return false;
        }

        return written == 16;
    }
}
