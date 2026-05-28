using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Rewrites HTTP response headers on the way from origin to client per RFC 7230 § 5.7.1
///     (Via) and § 6.1 (hop-by-hop header removal). Strips hop-by-hop headers that must not be
///     forwarded (Connection, Keep-Alive, Proxy-Authenticate, Proxy-Authorization, Proxy-Connection,
///     plus any header listed in the response's <c>Connection</c> header) and appends or extends
///     the <c>Via</c> chain with this proxy's identity.
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
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
        };
        AlwaysStrippedHeaders = headers;
    }

    /// <summary>
    ///     Returns a new <see cref="HypertextTransferProtocolResponseData" /> with hop-by-hop
    ///     headers stripped and the <c>Via</c> chain extended with this proxy's identity. The
    ///     original response is returned unchanged when no rewriting was needed.
    /// </summary>
    /// <param name="response">The response received from upstream.</param>
    /// <returns>A response with safely rewritten headers ready to forward to the client.</returns>
    public static HypertextTransferProtocolResponseData Rewrite(HypertextTransferProtocolResponseData response)
    {
        var connectionListedHeaders = ExtractConnectionListedHeaderNames(response.Headers);
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

    private static HashSet<string> ExtractConnectionListedHeaderNames(HeaderCollection headers)
    {
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = headers.Get("Connection");

        if (string.IsNullOrEmpty(connection))
        {
            return listed;
        }

        var tokens = connection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            listed.Add(token);
        }

        return listed;
    }
}
