using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Response-side rewriter for HTTP/1.1 <c>Upgrade</c> responses (typically <c>101 Switching
///     Protocols</c>). Preserves the <c>Upgrade</c> header and emits a sanitized <c>Connection</c>
///     header that only retains the <c>upgrade</c> control token needed for the handshake, while
///     stripping <c>Proxy-Authenticate</c>, <c>Proxy-Authorization</c>, <c>Proxy-Connection</c>,
///     and <c>Keep-Alive</c>, dropping any additional headers listed in the upstream
///     <c>Connection</c> header (RFC 7230 § 6.1 hop-by-hop) other than <c>Upgrade</c> itself, and
///     appending the <c>Via: 1.1 proxyfan</c> token (RFC 7230 § 5.7.1).
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
    ///     headers stripped, the <c>Connection</c> header sanitized to the preserved control
    ///     tokens, and the <c>Via</c> chain extended with this proxy's identity.
    /// </summary>
    /// <param name="response">The upstream upgrade response.</param>
    /// <returns>The rewritten response suitable for forwarding to the client.</returns>
    public static HypertextTransferProtocolResponseData Rewrite(HypertextTransferProtocolResponseData response)
    {
        var classification = ClassifyConnectionTokens(response.Headers);
        var sanitized = BuildSanitizedHeaders(response.Headers, classification);

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

    private static HeaderCollection BuildSanitizedHeaders(
        HeaderCollection source,
        ConnectionTokenClassification classification)
    {
        var sanitized = HeaderCollection.Empty;
        var existingViaChain = string.Empty;

        foreach (var header in source)
        {
            if (CanDrop(header.Key, classification.ListedHeaderNames))
            {
                continue;
            }

            if (string.Equals(header.Key, "Via", StringComparison.OrdinalIgnoreCase))
            {
                existingViaChain = string.Join(", ", header.Value);
                continue;
            }

            foreach (var value in header.Value)
            {
                sanitized = sanitized.Add(header.Key, value);
            }
        }

        if (classification.PreservedConnectionTokens.Count > 0)
        {
            sanitized = sanitized.Add("Connection", string.Join(", ", classification.PreservedConnectionTokens));
        }

        var viaValue = existingViaChain.Length > 0
            ? existingViaChain + ", " + ProxyViaIdentity
            : ProxyViaIdentity;
        return sanitized.Add("Via", viaValue);
    }

    private static bool CanDrop(string headerName, HashSet<string> listedHeaderNames)
    {
        if (AlwaysStrippedHeaders.Contains(headerName))
        {
            return true;
        }

        if (string.Equals(headerName, "Connection", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return listedHeaderNames.Contains(headerName);
    }

    private static ConnectionTokenClassification ClassifyConnectionTokens(HeaderCollection headers)
    {
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var preserved = new List<string>();

        foreach (var token in ConnectionHeaderTokenizer.Parse(headers))
        {
            if (string.Equals(token, "Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(token, "Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                preserved.Add(token);
                continue;
            }

            listed.Add(token);
        }

        return new ConnectionTokenClassification(listed, preserved);
    }

    private sealed class ConnectionTokenClassification
    {
        public HashSet<string> ListedHeaderNames { get; }

        public List<string> PreservedConnectionTokens { get; }

        public ConnectionTokenClassification(HashSet<string> listedHeaderNames, List<string> preservedConnectionTokens)
        {
            ListedHeaderNames = listedHeaderNames;
            PreservedConnectionTokens = preservedConnectionTokens;
        }
    }
}
