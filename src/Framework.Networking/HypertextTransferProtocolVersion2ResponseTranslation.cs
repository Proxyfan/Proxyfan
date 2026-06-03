using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     RFC 7540 § 8.1.2.4 — translation of a proxy HTTP/1.1
///     <see cref="HypertextTransferProtocolResponseData" /> into the HPACK-encoded header list
///     and body payload that an HTTP/2 server emits for a stream. The translator hoists the
///     status code onto a <c>:status</c> pseudo-header (which must precede regular headers),
///     lowercases header names (HTTP/2 wire format requirement, § 8.1.2), strips the
///     connection-specific headers (<c>Connection</c>, <c>Keep-Alive</c>, <c>Proxy-Connection</c>,
///     <c>Transfer-Encoding</c>, <c>Upgrade</c>) HTTP/2 forbids, and strips any extension
///     hop-by-hop headers named in the <c>Connection</c> header value (RFC 7230 § 6.1).
/// </summary>
public static class HypertextTransferProtocolVersion2ResponseTranslation
{
    private const string StatusPseudoHeader = ":status";
    private static readonly HashSet<string> ForbiddenConnectionHeaders;

    static HypertextTransferProtocolVersion2ResponseTranslation()
    {
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connection",
            "Keep-Alive",
            "Proxy-Connection",
            "Transfer-Encoding",
            "Upgrade",
        };
        ForbiddenConnectionHeaders = forbidden;
    }

    /// <summary>
    ///     Translates an HTTP/1.1 response into an HTTP/2 header list and body. The first entry
    ///     of the returned list is always the <c>:status</c> pseudo-header; remaining entries
    ///     are lowercase header names from the response (multiple values are expanded to one
    ///     entry per value, in the order they were stored).
    /// </summary>
    /// <param name="response">The HTTP/1.1 response to translate.</param>
    /// <returns>The translated HTTP/2 header list and body view.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="response" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When the response status code is outside [100, 999].</exception>
    public static HypertextTransferProtocolVersion2ResponseTranslationResult Translate(
        HypertextTransferProtocolResponseData response)
    {
        if (response.StatusCode is < 100 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(response), response.StatusCode, "HTTP/2 :status must be a 3-digit value.");
        }
        var headers = new List<HypertextTransferProtocolVersion2HpackHeaderField>(response.Headers.Count + 1);
        var statusValue = response.StatusCode.ToString(CultureInfo.InvariantCulture);
        var statusField = new HypertextTransferProtocolVersion2HpackHeaderField(StatusPseudoHeader, statusValue);
        headers.Add(statusField);
        AppendRegularHeaders(response.Headers, headers);
        var result = new HypertextTransferProtocolVersion2ResponseTranslationResult(headers, response.Body);
        return result;
    }

    private static void AppendRegularHeaders(
        HeaderCollection source,
        List<HypertextTransferProtocolVersion2HpackHeaderField> destination)
    {
        var connectionTokens = ConnectionHeaderTokenizer.Parse(source);
        HashSet<string>? extensionHopByHop = null;
        if (connectionTokens.Length > 0)
        {
            var hopByHopSet = new HashSet<string>(connectionTokens, StringComparer.OrdinalIgnoreCase);
            extensionHopByHop = hopByHopSet;
        }

        foreach (var pair in source)
        {
            var name = pair.Key;
            if (ForbiddenConnectionHeaders.Contains(name))
            {
                continue;
            }
            if (extensionHopByHop is not null && extensionHopByHop.Contains(name))
            {
                continue;
            }
            var lowercaseName = name.ToLowerInvariant();
            var values = pair.Value;
            for (var index = 0; index < values.Length; index++)
            {
                var field = new HypertextTransferProtocolVersion2HpackHeaderField(lowercaseName, values[index]);
                destination.Add(field);
            }
        }
    }
}
