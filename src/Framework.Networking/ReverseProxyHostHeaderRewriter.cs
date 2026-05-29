using Proxyfan.Domain.Traffic;
using System;
using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Rewrites the <c>Host</c> header on a request being forwarded by a reverse proxy route to
///     point at the configured backend (so the backend sees a Host header it recognizes rather
///     than the route's external listen address). Operates structurally over
///     <see cref="HeaderCollection" /> so other request semantics — method, URI, body, version —
///     are preserved exactly. When the request has no <c>Host</c> header (HTTP/1.0 or absolute
///     URI request line) one is added; otherwise the existing value is replaced.
/// </summary>
public static class ReverseProxyHostHeaderRewriter
{
    private const int DefaultHypertextTransferProtocolPort = 80;
    private const int DefaultHypertextTransferProtocolSecurePort = 443;
    private const string HostHeaderName = "Host";

    /// <summary>
    ///     Returns a new request with the <c>Host</c> header set to
    ///     <c>backendHost</c> (omitting the port when it is the default HTTP or HTTPS port).
    /// </summary>
    /// <param name="request">The request to rewrite.</param>
    /// <param name="backendHost">The backend host name.</param>
    /// <param name="backendPort">The backend TCP port.</param>
    /// <returns>A new request with the Host header rewritten.</returns>
    public static HypertextTransferProtocolRequestData Rewrite(
        HypertextTransferProtocolRequestData request,
        string backendHost,
        int backendPort)
    {
        var hostValue = BuildHostValue(backendHost, backendPort);
        var rewritten = HeaderCollection.Empty;
        var hostReplaced = false;

        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, HostHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                if (!hostReplaced)
                {
                    rewritten = rewritten.Add(HostHeaderName, hostValue);
                    hostReplaced = true;
                }

                continue;
            }

            foreach (var value in header.Value)
            {
                rewritten = rewritten.Add(header.Key, value);
            }
        }

        if (!hostReplaced)
        {
            rewritten = rewritten.Add(HostHeaderName, hostValue);
        }

        var parameters = new HypertextTransferProtocolRequestDataParameters
        {
            Body = request.Body,
            Headers = rewritten,
            Method = request.Method,
            RequestUri = request.RequestUri,
            Version = request.Version,
        };
        return new HypertextTransferProtocolRequestData(parameters);
    }

    private static string BuildHostValue(string backendHost, int backendPort)
    {
        if (backendPort is DefaultHypertextTransferProtocolPort or DefaultHypertextTransferProtocolSecurePort)
        {
            return backendHost;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{backendHost}:{backendPort}");
    }
}
