using Proxyfan.Domain.Certificates.Provisioning;
using Proxyfan.Domain.Traffic;
using System;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Detects requests addressed to the magic provisioning host and serves the local
///     root certificate authority install pages directly from the proxy. Pure helper
///     class with no instance state — registered as a static gate inside the HTTP
///     proxy handler.
/// </summary>
public static class CertificateProvisioningResponder
{
    /// <summary>
    ///     The DNS-style host name clients use to reach the certificate provisioning
    ///     landing page through the proxy. Browsers can navigate to
    ///     <c>http://proxyfan.proxy/</c> while configured to use Proxyfan as their
    ///     HTTP proxy.
    /// </summary>
    public const string MagicHost = "proxyfan.proxy";

    /// <summary>
    ///     Builds the wire-ready response data for the supplied provisioning request.
    ///     Inspect <see cref="HypertextTransferProtocolResponseData.Headers" /> for the
    ///     content disposition and content type that have already been set.
    /// </summary>
    /// <param name="request">The HTTP request that targeted the provisioning host.</param>
    /// <param name="certificate">The current root certificate authority certificate.</param>
    /// <returns>The synthesized response payload to send back to the client.</returns>
    public static HypertextTransferProtocolResponseData BuildResponse(
        HypertextTransferProtocolRequestData request,
        X509Certificate2 certificate)
    {
        var path = ExtractPath(request.RequestUri);
        var payload = CertificateProvisioningResponseBuilder.Build(path, certificate);
        var headers = HeaderCollection.Empty
            .Add("Cache-Control", "no-store")
            .Add("Connection", "close")
            .Add("Content-Length", payload.Body.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add("Content-Type", payload.ContentType);
        if (!string.IsNullOrEmpty(payload.ContentDisposition))
        {
            headers = headers.Add("Content-Disposition", payload.ContentDisposition);
        }

        var parameters = new HypertextTransferProtocolResponseDataParameters
        {
            Body = payload.Body.ToArray(),
            Headers = headers,
            ReasonPhrase = "OK",
            StatusCode = 200,
            Version = "HTTP/1.1",
        };
        var response = new HypertextTransferProtocolResponseData(parameters);
        return response;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied request targets the magic
    ///     provisioning host (matched against the Host header).
    /// </summary>
    /// <param name="request">The parsed HTTP request.</param>
    /// <returns>True when the request should be served the provisioning page.</returns>
    public static bool HasProvisioningTarget(HypertextTransferProtocolRequestData request)
    {
        var hostValue = request.Headers.Get("Host");
        if (string.IsNullOrWhiteSpace(hostValue))
        {
            return false;
        }

        var separatorIndex = hostValue.IndexOf(':', StringComparison.Ordinal);
        var hostOnly = separatorIndex < 0 ? hostValue : hostValue[..separatorIndex];
        return string.Equals(hostOnly, MagicHost, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Serializes the supplied response and writes it to the client output pipe.
    /// </summary>
    /// <param name="output">The pipe writer that owns the client transport output.</param>
    /// <param name="response">The response payload (typically from <see cref="BuildResponse" />).</param>
    /// <param name="cancellationToken">Token that cancels the write.</param>
    /// <returns>A task that completes once the response has been flushed.</returns>
    public static async Task WriteResponseAsync(
        PipeWriter output,
        HypertextTransferProtocolResponseData response,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.Append(response.Version).Append(' ').Append(response.StatusCode).Append(' ').Append(response.ReasonPhrase).Append("\r\n");
        foreach (var header in response.Headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key).Append(": ").Append(value).Append("\r\n");
            }
        }

        builder.Append("\r\n");
        var headerBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await output.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        if (response.Body.Length > 0)
        {
            await output.WriteAsync(response.Body, cancellationToken).ConfigureAwait(false);
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ExtractPath(Uri requestUri)
    {
        if (requestUri is null)
        {
            return "/";
        }

        if (requestUri.IsAbsoluteUri)
        {
            return requestUri.PathAndQuery;
        }

        var raw = requestUri.OriginalString;
        if (string.IsNullOrEmpty(raw))
        {
            return "/";
        }

        if (raw[0] == '/')
        {
            return raw;
        }

        return "/" + raw;
    }
}
