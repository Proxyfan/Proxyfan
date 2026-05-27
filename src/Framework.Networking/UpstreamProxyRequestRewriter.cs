using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds the wire bytes for an HTTP request being forwarded through an upstream proxy.
///     The request line is rewritten to use the absolute URI form expected by HTTP/1.1 proxies
///     (e.g. <c>GET http://example.com/path HTTP/1.1</c> rather than <c>GET /path HTTP/1.1</c>).
///     When a <c>Proxy-Authorization</c> header value is supplied it is injected (replacing any
///     existing one) immediately after the rewritten request line.
/// </summary>
public static class UpstreamProxyRequestRewriter
{
    private const string ProxyAuthorizationHeaderName = "Proxy-Authorization";

    /// <summary>
    ///     Returns the header bytes rewritten with an absolute-URI request line. The body bytes
    ///     are unchanged and should be written after the returned header bytes.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version).</param>
    /// <returns>The rewritten header bytes.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request)
    {
        return RewriteHeaders(originalHeaderBytes, request, proxyAuthorization: null);
    }

    /// <summary>
    ///     Returns the header bytes rewritten with an absolute-URI request line and, when
    ///     <paramref name="proxyAuthorization" /> is non-null, an injected
    ///     <c>Proxy-Authorization</c> header. Any pre-existing <c>Proxy-Authorization</c>
    ///     header in the original bytes is stripped so the upstream sees exactly the supplied
    ///     credentials.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version).</param>
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
        var newLineBytes = Encoding.ASCII.GetBytes(newRequestLine);

        if (proxyAuthorization is null)
        {
            var preservedHeaderSection = span[firstLineEnd..];
            var rewritten = new byte[newLineBytes.Length + preservedHeaderSection.Length];
            newLineBytes.CopyTo(rewritten.AsSpan());
            preservedHeaderSection.CopyTo(rewritten.AsSpan(newLineBytes.Length));
            return rewritten;
        }

        var headerSection = StripExistingProxyAuthorization(span[firstLineEnd..]);
        var authLine = $"\r\n{ProxyAuthorizationHeaderName}: {proxyAuthorization}";
        var authBytes = Encoding.ASCII.GetBytes(authLine);
        var rewrittenWithAuth = new byte[newLineBytes.Length + authBytes.Length + headerSection.Length];
        newLineBytes.CopyTo(rewrittenWithAuth.AsSpan());
        authBytes.CopyTo(rewrittenWithAuth.AsSpan(newLineBytes.Length));
        headerSection.CopyTo(rewrittenWithAuth.AsSpan(newLineBytes.Length + authBytes.Length));
        return rewrittenWithAuth;
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

    private static byte[] StripExistingProxyAuthorization(ReadOnlySpan<byte> headersIncludingLeadingCarriageReturnLineFeed)
    {
        var text = Encoding.ASCII.GetString(headersIncludingLeadingCarriageReturnLineFeed);
        var lines = text.Split("\r\n");
        var filtered = new System.Collections.Generic.List<string>(lines.Length);

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex > 0)
            {
                var name = line[..colonIndex];

                if (string.Equals(name, ProxyAuthorizationHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            filtered.Add(line);
        }

        var rebuilt = string.Join("\r\n", filtered);
        return Encoding.ASCII.GetBytes(rebuilt);
    }
}
