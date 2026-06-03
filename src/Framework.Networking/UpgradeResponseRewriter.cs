using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Response-side rewriter for HTTP/1.1 <c>Upgrade</c> responses (typically <c>101 Switching
///     Protocols</c>). Preserves the <c>Connection</c> and <c>Upgrade</c> headers so the client
///     sees the upgrade acknowledgment intact, while still stripping <c>Proxy-Authenticate</c>,
///     <c>Proxy-Authorization</c>, <c>Proxy-Connection</c>, and <c>Keep-Alive</c>, dropping any
///     additional hop-by-hop headers listed in the response <c>Connection</c> header (other than
///     <c>Connection</c>/<c>Upgrade</c> themselves), and appending the <c>Via: 1.1 proxyfan</c>
///     token (RFC 7230 § 5.7.1, § 6.1).
/// </summary>
public static class UpgradeResponseRewriter
{
    private const string ProxyViaIdentity = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static UpgradeResponseRewriter()
    {
        var stripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
        };
        AlwaysStrippedHeaders = stripped;
    }

    /// <summary>
    ///     Returns a new <see cref="HypertextTransferProtocolResponseData" /> with hop-by-hop
    ///     headers (other than <c>Connection</c>/<c>Upgrade</c>) stripped and the <c>Via</c>
    ///     chain extended with this proxy's identity.
    /// </summary>
    /// <param name="response">The upstream upgrade response.</param>
    /// <returns>The rewritten response suitable for forwarding to the client.</returns>
    public static HypertextTransferProtocolResponseData Rewrite(HypertextTransferProtocolResponseData response)
    {
        var sanitized = HeaderCollection.Empty;
        var hasExistingVia = false;
        var existingViaChain = string.Empty;
        var connectionListedHeaders = ExtractConnectionListedHeaders(response.Headers);

        foreach (var header in response.Headers)
        {
            if (AlwaysStrippedHeaders.Contains(header.Key))
            {
                continue;
            }

            if (string.Equals(header.Key, "Via", StringComparison.OrdinalIgnoreCase))
            {
                existingViaChain = string.Join(", ", header.Value);
                hasExistingVia = existingViaChain.Length > 0;
                continue;
            }

            if (connectionListedHeaders.Contains(header.Key)
                && !string.Equals(header.Key, "Connection", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(header.Key, "Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in header.Value)
            {
                sanitized = sanitized.Add(header.Key, value);
            }
        }

        var viaValue = hasExistingVia ? existingViaChain + ", " + ProxyViaIdentity : ProxyViaIdentity;
        sanitized = sanitized.Add("Via", viaValue);

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = response.Body,
            Headers = sanitized,
            ReasonPhrase = response.ReasonPhrase,
            StatusCode = response.StatusCode,
            Version = response.Version,
        };
        return new HypertextTransferProtocolResponseData(parameters);
    }

    private static HashSet<string> ExtractConnectionListedHeaders(HeaderCollection headers)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in headers.GetAll("Connection"))
        {
            foreach (var rawToken in value.Split(','))
            {
                var token = rawToken.Trim();

                if (token.Length > 0)
                {
                    tokens.Add(token);
                }
            }
        }

        return tokens;
    }
}
