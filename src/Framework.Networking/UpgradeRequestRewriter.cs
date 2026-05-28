using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Specialized request-line/header rewriter for HTTP/1.1 <c>Upgrade</c> requests. Unlike
///     <see cref="OriginRequestRewriter" />, this rewriter preserves the <c>Connection</c> and
///     <c>Upgrade</c> headers (they carry the WebSocket handshake semantics that must reach
///     the upstream server intact) while still stripping <c>Proxy-Authenticate</c>,
///     <c>Proxy-Authorization</c>, and <c>Proxy-Connection</c> (security: never leak proxy
///     credentials to origin) and appending the <c>Via: 1.1 proxyfan</c> token.
/// </summary>
public static class UpgradeRequestRewriter
{
    private const string ViaToken = "1.1 proxyfan";
    private static readonly HashSet<string> AlwaysStrippedHeaders;

    static UpgradeRequestRewriter()
    {
        var alwaysStripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
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

        var rebuilt = new StringBuilder(text.Length + ViaToken.Length + 16);
        rebuilt.Append(rewrittenRequestLine);
        rebuilt.Append("\r\n");
        var viaInline = HasInlineVia(headerLines);
        AppendHeaderLines(headerLines, rebuilt);

        if (!viaInline)
        {
            rebuilt.Append("Via: ");
            rebuilt.Append(ViaToken);
            rebuilt.Append("\r\n");
        }

        rebuilt.Append("\r\n");
        return Encoding.ASCII.GetBytes(rebuilt.ToString());
    }

    private static void AppendHeaderLines(string[] headerLines, StringBuilder rebuilt)
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

            if (AlwaysStrippedHeaders.Contains(name))
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
