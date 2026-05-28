using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     RFC 7540 § 8.1.2 — translation between an HTTP/2 request header list (decoded by HPACK)
///     and the proxy's HTTP/1.1 <see cref="HypertextTransferProtocolRequestData" /> shape. The
///     translator preserves request semantics by hoisting the four standard pseudo-headers
///     (<c>:method</c>, <c>:scheme</c>, <c>:authority</c>, <c>:path</c>) onto the request URI
///     and method, normalises the Host header from <c>:authority</c>, and strips the
///     connection-specific headers (<c>Connection</c>, <c>Keep-Alive</c>, <c>Proxy-Connection</c>,
///     <c>Transfer-Encoding</c>, <c>Upgrade</c>) that HTTP/2 forbids per § 8.1.2.2.
/// </summary>
public static class HypertextTransferProtocolVersion2RequestTranslation
{
    private const string AuthorityPseudoHeader = ":authority";
    private const string DefaultHttpsScheme = "https";
    private const string MethodPseudoHeader = ":method";
    private const string PathPseudoHeader = ":path";
    private const string SchemePseudoHeader = ":scheme";
    private const string Version2String = "HTTP/2";
    private static readonly HashSet<string> ForbiddenConnectionHeaders;

    static HypertextTransferProtocolVersion2RequestTranslation()
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
    ///     Translates an HTTP/2 header list and body into an HTTP/1.1 request data instance.
    ///     The returned request preserves the original body and carries the HTTP/2 version
    ///     string so downstream consumers can record the on-the-wire protocol.
    /// </summary>
    /// <param name="headers">The decoded HPACK header list (pseudo-headers must precede regular headers).</param>
    /// <param name="body">The request body bytes accumulated from DATA frames.</param>
    /// <returns>
    ///     A populated <see cref="HypertextTransferProtocolRequestData" /> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">When <paramref name="headers" /> is <c>null</c>.</exception>
    /// <exception cref="FormatException">When required pseudo-headers are missing or invalid.</exception>
    public static HypertextTransferProtocolRequestData Translate(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        ReadOnlyMemory<byte> body)
    {
        var pseudo = ExtractPseudoHeaders(headers);
        var headerCollection = BuildHeaderCollection(headers, pseudo.Authority);
        var requestUri = BuildRequestUri(pseudo);
        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = body,
            Headers = headerCollection,
            Method = pseudo.Method,
            RequestUri = requestUri,
            Version = Version2String,
        };
        var request = new HypertextTransferProtocolRequestData(parameters);
        return request;
    }

    private static HeaderCollection BuildHeaderCollection(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers,
        string authority)
    {
        var collection = HeaderCollection.Empty;
        var hasHostHeader = false;
        for (var index = 0; index < headers.Count; index++)
        {
            var field = headers[index];
            var name = field.Name;
            if (name.StartsWith(':'))
            {
                continue;
            }
            if (ForbiddenConnectionHeaders.Contains(name))
            {
                continue;
            }
            if (string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase))
            {
                hasHostHeader = true;
            }
            collection = collection.Add(name, field.Value);
        }
        if (!hasHostHeader)
        {
            collection = collection.Add("Host", authority);
        }
        return collection;
    }

    private static Uri BuildRequestUri(PseudoHeaders pseudo)
    {
        var scheme = string.IsNullOrEmpty(pseudo.Scheme) ? DefaultHttpsScheme : pseudo.Scheme;
        var authority = pseudo.Authority;
        var path = string.IsNullOrEmpty(pseudo.Path) ? "/" : pseudo.Path;
        var formatted = string.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}", scheme, authority, path);
        if (!Uri.TryCreate(formatted, UriKind.Absolute, out var requestUri))
        {
            throw new FormatException("HTTP/2 pseudo-headers do not form a valid absolute request URI.");
        }
        return requestUri;
    }

    private static PseudoHeaders ExtractPseudoHeaders(
        IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> headers)
    {
        string? method = null;
        string? scheme = null;
        string? authority = null;
        string? path = null;
        for (var index = 0; index < headers.Count; index++)
        {
            var field = headers[index];
            var name = field.Name;
            if (string.Equals(name, MethodPseudoHeader, StringComparison.Ordinal))
            {
                method = field.Value;
            }
            else if (string.Equals(name, SchemePseudoHeader, StringComparison.Ordinal))
            {
                scheme = field.Value;
            }
            else if (string.Equals(name, AuthorityPseudoHeader, StringComparison.Ordinal))
            {
                authority = field.Value;
            }
            else if (string.Equals(name, PathPseudoHeader, StringComparison.Ordinal))
            {
                path = field.Value;
            }
        }
        if (string.IsNullOrEmpty(method))
        {
            throw new FormatException("HTTP/2 request is missing the required :method pseudo-header.");
        }
        if (string.IsNullOrEmpty(authority))
        {
            throw new FormatException("HTTP/2 request is missing the required :authority pseudo-header.");
        }
        var pseudo = new PseudoHeaders(method, scheme ?? string.Empty, authority, path ?? string.Empty);
        return pseudo;
    }

    private readonly struct PseudoHeaders
    {
        public string Authority { get; }

        public string Method { get; }

        public string Path { get; }

        public string Scheme { get; }

        public PseudoHeaders(string method, string scheme, string authority, string path)
        {
            Method = method;
            Scheme = scheme;
            Authority = authority;
            Path = path;
        }
    }
}
