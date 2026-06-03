using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Specialized request-line/header rewriter for HTTP/1.1 <c>Upgrade</c> requests. Unlike
///     <see cref="OriginRequestRewriter" />, this rewriter preserves the <c>Connection</c> and
///     <c>Upgrade</c> headers (they carry the WebSocket handshake semantics that must reach
///     the upstream server intact) while still stripping <c>Proxy-Authenticate</c>,
///     <c>Proxy-Authorization</c>, <c>Proxy-Connection</c>, and other standard hop-by-hop
///     headers (<c>Keep-Alive</c>, <c>TE</c>, <c>Trailer</c>, <c>Transfer-Encoding</c>) plus
///     any header names listed in the client's <c>Connection</c> header value (other than the
///     <c>upgrade</c>/<c>close</c>/<c>keep-alive</c> tokens), so connection-scoped controls
///     never leak to the origin. Body framing is normalized: <c>Content-Length</c> is stripped
///     from the inbound headers and, when the request carries a decoded body, a fresh
///     <c>Content-Length</c> matching the body length is injected. The <c>Via: 1.1 proxyfan</c>
///     token is appended per RFC 7230.
/// </summary>
public static class UpgradeRequestRewriter
{
    private const string ViaToken = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static UpgradeRequestRewriter()
    {
        var alwaysStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Length",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
            "TE",
            "Trailer",
            "Transfer-Encoding",
        };
        AlwaysStrippedHeaders = alwaysStripped;
    }

    /// <summary>
    ///     Returns the rewritten upgrade request header bytes ready to send upstream.
    /// </summary>
    /// <param name="originalHeaderBytes">The original request header bytes from the client.</param>
    /// <param name="request">The parsed request data used for the request line.</param>
    /// <returns>The rewritten header bytes ending with the CRLF CRLF terminator.</returns>
    public static byte[] RewriteHeaders(
        ReadOnlyMemory<byte> originalHeaderBytes,
        HypertextTransferProtocolRequestData request)
    {
        var text = Encoding.ASCII.GetString(originalHeaderBytes.Span);
        var firstLineEnd = text.IndexOf("\r\n", StringComparison.Ordinal);

        if (firstLineEnd < 0)
        {
            return originalHeaderBytes.ToArray();
        }

        var originForm = BuildOriginForm(request);
        var rewrittenRequestLine = $"{request.Method} {originForm} {request.Version}";
        var headerSection = text[(firstLineEnd + 2)..];
        var headerLines = headerSection.Split("\r\n", StringSplitOptions.None);
        var connectionListedNames = ExtractConnectionListedHeaderNames(headerLines);

        var rebuilt = new StringBuilder(text.Length + ViaToken.Length + 16);
        rebuilt.Append(rewrittenRequestLine);
        rebuilt.Append("\r\n");
        var viaInline = HasInlineVia(headerLines);
        AppendHeaderLines(headerLines, connectionListedNames, rebuilt);

        if (!viaInline)
        {
            rebuilt.Append("Via: ");
            rebuilt.Append(ViaToken);
            rebuilt.Append("\r\n");
        }

        if (request.Body.Length > 0)
        {
            rebuilt.Append("Content-Length: ");
            rebuilt.Append(request.Body.Length.ToString(CultureInfo.InvariantCulture));
            rebuilt.Append("\r\n");
        }

        rebuilt.Append("\r\n");
        return Encoding.ASCII.GetBytes(rebuilt.ToString());
    }

    private static void AppendHeaderLines(string[] headerLines, HashSet<string> connectionListedNames, StringBuilder rebuilt)
    {
        foreach (var line in headerLines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var colonIndex = line.IndexOf(':');

            if (colonIndex <= 0)
            {
                rebuilt.Append(line);
                rebuilt.Append("\r\n");
                continue;
            }

            var name = line[..colonIndex].Trim();

            if (AlwaysStrippedHeaders.Contains(name) || connectionListedNames.Contains(name))
            {
                continue;
            }

            if (string.Equals(name, "Via", StringComparison.OrdinalIgnoreCase))
            {
                rebuilt.Append(line.TrimEnd());
                rebuilt.Append(", ");
                rebuilt.Append(ViaToken);
                rebuilt.Append("\r\n");
                continue;
            }

            rebuilt.Append(line);
            rebuilt.Append("\r\n");
        }
    }

    private static string BuildOriginForm(HypertextTransferProtocolRequestData request)
    {
        if (request.RequestUri.IsAbsoluteUri)
        {
            var pathAndQuery = request.RequestUri.PathAndQuery;
            return string.IsNullOrEmpty(pathAndQuery) ? "/" : pathAndQuery;
        }

        var originalPath = request.RequestUri.OriginalString;

        if (string.IsNullOrEmpty(originalPath))
        {
            return "/";
        }

        var fragmentIndex = originalPath.IndexOf('#');

        if (fragmentIndex >= 0)
        {
            originalPath = originalPath[..fragmentIndex];
        }

        return string.IsNullOrEmpty(originalPath) ? "/" : originalPath;
    }

    private static HashSet<string> ExtractConnectionListedHeaderNames(string[] headerLines)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in headerLines)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex <= 0)
            {
                continue;
            }

            var name = line[..colonIndex].Trim();

            if (!string.Equals(name, "Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = line[(colonIndex + 1)..].Trim();
            var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                if (string.Equals(token, "close", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token, "keep-alive", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token, "upgrade", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                names.Add(token);
            }
        }

        return names;
    }

    private static bool HasInlineVia(string[] headerLines)
    {
        foreach (var line in headerLines)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex <= 0)
            {
                continue;
            }

            var name = line[..colonIndex].Trim();

            if (string.Equals(name, "Via", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
