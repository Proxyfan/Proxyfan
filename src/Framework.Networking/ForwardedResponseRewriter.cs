using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Rewrites HTTP response headers on the way from origin to client per RFC 7230 § 5.7.1
///     (Via) and § 6.1 (hop-by-hop header removal). Strips the RFC 7230 § 6.1 hop-by-hop
///     header set (Connection, Keep-Alive, Proxy-Authenticate, Proxy-Authorization, TE,
///     Trailer, Transfer-Encoding, Upgrade), plus the widely-deployed non-standard
///     <c>Proxy-Connection</c> header and any header listed in the response's
///     <c>Connection</c> header. Appends or extends the <c>Via</c> chain with this proxy's
///     identity. Normalizes body framing by resetting <c>Content-Length</c> to the decoded
///     body length (chunked-decoded bodies must not be re-emitted under chunked framing).
/// </summary>
public static class ForwardedResponseRewriter
{
    private const string ProxyViaIdentity = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static ForwardedResponseRewriter()
    {
        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connection",
            "Content-Length",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
        };
        AlwaysStrippedHeaders = headers;
    }

    /// <summary>
    ///     Returns a new <see cref="HypertextTransferProtocolResponseData" /> with hop-by-hop
    ///     headers stripped, body framing normalized to <c>Content-Length</c> matching the
    ///     decoded body length, and the <c>Via</c> chain extended with this proxy's identity.
    /// </summary>
    /// <param name="response">The response received from upstream.</param>
    /// <returns>A response with safely rewritten headers ready to forward to the client.</returns>
    public static HypertextTransferProtocolResponseData Rewrite(HypertextTransferProtocolResponseData response)
    {
        var connectionListedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in ConnectionHeaderTokenizer.Parse(response.Headers))
        {
            connectionListedHeaders.Add(token);
        }

        var sanitized = HeaderCollection.Empty;
        var hasExistingVia = false;
        string existingViaChain = string.Empty;

        foreach (var header in response.Headers)
        {
            if (AlwaysStrippedHeaders.Contains(header.Key))
            {
                continue;
            }

            if (connectionListedHeaders.Contains(header.Key))
            {
                continue;
            }

            if (string.Equals(header.Key, "Via", StringComparison.OrdinalIgnoreCase))
            {
                existingViaChain = string.Join(", ", header.Value);
                hasExistingVia = existingViaChain.Length > 0;
                continue;
            }

            foreach (var value in header.Value)
            {
                sanitized = sanitized.Add(header.Key, value);
            }
        }

        sanitized = sanitized.Add("Content-Length", response.Body.Length.ToString(CultureInfo.InvariantCulture));
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

}
