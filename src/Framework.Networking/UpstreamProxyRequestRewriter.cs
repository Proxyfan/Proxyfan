using Proxyfan.Domain.Traffic;
using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds the wire bytes for an HTTP request being forwarded through an upstream proxy.
///     The request line is rewritten to use the absolute URI form expected by HTTP/1.1 proxies
///     (e.g. <c>GET http://example.com/path HTTP/1.1</c> rather than <c>GET /path HTTP/1.1</c>).
/// </summary>
public static class UpstreamProxyRequestRewriter
{
    /// <summary>
    ///     Returns the header bytes rewritten with an absolute-URI request line. The body bytes
    ///     are unchanged and should be written after the returned header bytes.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data (used for method, URI, version).</param>
    /// <returns>The rewritten header bytes.</returns>
    public static byte[] RewriteHeaders(ReadOnlyMemory<byte> originalHeaderBytes, HypertextTransferProtocolRequestData request)
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
        var remainder = span[firstLineEnd..];
        var rewritten = new byte[newLineBytes.Length + remainder.Length];
        newLineBytes.CopyTo(rewritten.AsSpan());
        remainder.CopyTo(rewritten.AsSpan(newLineBytes.Length));
        return rewritten;
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
