using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds the wire bytes for an HTTP request being forwarded through an upstream proxy.
///     The request line is rewritten to use the absolute URI form expected by HTTP/1.1 proxies
///     (e.g. <c>GET http://example.com/path HTTP/1.1</c> rather than <c>GET /path HTTP/1.1</c>).
///     Any inbound <c>Proxy-Authorization</c> header is always stripped (RFC 9110 §7.6.1 /
///     §11.7.1 — hop-by-hop credentials must not be forwarded); when a replacement value is
///     supplied it is injected. Body framing is normalized: <c>Transfer-Encoding</c> and
///     <c>Content-Length</c> are stripped from the inbound headers, and a fresh
///     <c>Content-Length</c> matching the decoded body length is injected when a body is
///     present (chunked-decoded bodies must not be re-emitted under chunked framing).
/// </summary>
public static class UpstreamProxyRequestRewriter
{
    private const string ProxyAuthorizationHeaderName = "Proxy-Authorization";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static UpstreamProxyRequestRewriter()
    {
        var alwaysStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Length",
            ProxyAuthorizationHeaderName,
            "Transfer-Encoding",
        };
        AlwaysStrippedHeaders = alwaysStripped;
    }

    /// <summary>
    ///     Returns the header bytes rewritten with an absolute-URI request line. The body bytes
    ///     are unchanged and should be written after the returned header bytes. Any inbound
    ///     <c>Proxy-Authorization</c> header is always stripped — it is a hop-by-hop credential
    ///     intended for the local Proxyfan hop only and must never leak to the upstream proxy
    ///     (RFC 9110 §11.7.1).
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version, body length).</param>
    /// <returns>The rewritten header bytes.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request)
    {
        return RewriteHeaders(originalHeaderBytes, request, proxyAuthorization: null);
    }

    /// <summary>
    ///     Returns the header bytes rewritten with an absolute-URI request line. Any
    ///     pre-existing <c>Proxy-Authorization</c> header in the original bytes is always
    ///     stripped, regardless of whether <paramref name="proxyAuthorization" /> is supplied —
    ///     the client's credential is intended for the local Proxyfan hop only and must never
    ///     leak to the upstream proxy (RFC 9110 §11.7.1). When
    ///     <paramref name="proxyAuthorization" /> is non-null, the configured value is then
    ///     injected. Inbound <c>Transfer-Encoding</c> and <c>Content-Length</c> are always
    ///     stripped; when the request carries a decoded body a fresh <c>Content-Length</c>
    ///     matching the body length is injected.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version, body length).</param>
    /// <param name="proxyAuthorization">
    ///     The header value for <c>Proxy-Authorization</c>, or <see langword="null" /> to leave the
    ///     header omitted. Built by <see cref="ProxyAuthorizationHeader.Build" />.
    /// </param>
    /// <returns>The rewritten header bytes.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request, string? proxyAuthorization)
    {
        var span = originalHeaderBytes.Span;
        var firstLineEnd = IndexOfCarriageReturnLineFeed(span);

        if (firstLineEnd < 0)
        {
            return originalHeaderBytes.ToArray();
        }

        var absoluteUri = BuildAbsoluteRequestUri(request);
        var newRequestLine = $"{request.Method} {absoluteUri} {request.Version}";

        var headerSection = Encoding.ASCII.GetString(span[(firstLineEnd + 2)..]);
        var rebuilt = new StringBuilder(headerSection.Length + newRequestLine.Length + 96);
        rebuilt.Append(newRequestLine);
        rebuilt.Append("\r\n");
        AppendFilteredHeaderLines(rebuilt, headerSection);
        AppendTrailingHeaders(rebuilt, request, proxyAuthorization);
        rebuilt.Append("\r\n");
        return Encoding.ASCII.GetBytes(rebuilt.ToString());
    }

    private static void AppendFilteredHeaderLines(StringBuilder destination, string headerSection)
    {
        var lines = headerSection.Split("\r\n");

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var colonIndex = line.IndexOf(':');

            if (colonIndex > 0)
            {
                var name = line[..colonIndex].Trim();

                if (AlwaysStrippedHeaders.Contains(name))
                {
                    continue;
                }
            }

            destination.Append(line);
            destination.Append("\r\n");
        }
    }

    private static void AppendTrailingHeaders(StringBuilder destination, HypertextTransferProtocolRequestData request, string? proxyAuthorization)
    {
        if (proxyAuthorization is not null)
        {
            destination.Append(ProxyAuthorizationHeaderName);
            destination.Append(": ");
            destination.Append(proxyAuthorization);
            destination.Append("\r\n");
        }

        if (request.Body.Length > 0)
        {
            destination.Append("Content-Length: ");
            destination.Append(request.Body.Length.ToString(CultureInfo.InvariantCulture));
            destination.Append("\r\n");
        }
    }

    private static string BuildAbsoluteRequestUri(HypertextTransferProtocolRequestData request)
    {
        var requestUri = request.RequestUri;

        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.ToString();
        }

        var hostHeader = request.Headers.Get("Host") ?? "unknown";
        var path = requestUri.OriginalString;
        var absolute = $"http://{hostHeader}{path}";
        return absolute;
    }

    private static int IndexOfCarriageReturnLineFeed(ReadOnlySpan<byte> span)
    {
        for (var index = 0; index < span.Length - 1; index++)
        {
            if (span[index] == (byte)'\r' && span[index + 1] == (byte)'\n')
            {
                return index;
            }
        }

        return -1;
    }
}
