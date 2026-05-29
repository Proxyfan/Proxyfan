using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helpers shared by the TLS interceptor: traffic-flow construction,
///     keep-alive detection, and TLS authentication option construction.
/// </summary>
public static class TransportLayerSecurityInterceptorHelpers
{
    /// <summary>
    ///     Builds an <see cref="SslClientAuthenticationOptions" /> for use when the proxy
    ///     connects to the upstream server as a TLS client. Advertises both <c>h2</c> and
    ///     <c>http/1.1</c> via ALPN so the upstream can negotiate either protocol; the proxy
    ///     then mirrors the upstream's choice when accepting the intercepted client.
    /// </summary>
    /// <param name="target">The CONNECT target whose host is used for SNI.</param>
    /// <returns>A populated <see cref="SslClientAuthenticationOptions" />.</returns>
    public static SslClientAuthenticationOptions CreateClientTransportLayerSecurityOptions(ConnectTarget target)
    {
        var options = new SslClientAuthenticationOptions
        {
            ApplicationProtocols = [SslApplicationProtocol.Http2, SslApplicationProtocol.Http11],
            TargetHost = target.Host,
        };
        return options;
    }

    /// <summary>
    ///     Builds an <see cref="SslServerAuthenticationOptions" /> for use when the proxy
    ///     presents the per-host leaf certificate to the intercepted client. The supplied
    ///     <paramref name="negotiatedUpstreamProtocol" /> is offered exclusively so the client
    ///     negotiates the same protocol the upstream chose — keeping the MITM end-to-end on
    ///     either HTTP/1.1 or HTTP/2.
    /// </summary>
    /// <param name="leafCertificate">The leaf certificate signed by the proxy CA.</param>
    /// <param name="negotiatedUpstreamProtocol">
    ///     The ALPN protocol the upstream selected; offered exclusively to the client side so
    ///     the proxy negotiates the same protocol on both sides of the MITM.
    /// </param>
    /// <returns>A populated <see cref="SslServerAuthenticationOptions" />.</returns>
    public static SslServerAuthenticationOptions CreateServerTransportLayerSecurityOptions(X509Certificate2 leafCertificate, SslApplicationProtocol negotiatedUpstreamProtocol)
    {
        List<SslApplicationProtocol> protocols;
        if (negotiatedUpstreamProtocol.Protocol.IsEmpty)
        {
            List<SslApplicationProtocol> fallback =
            [
                SslApplicationProtocol.Http11,
            ];
            protocols = fallback;
        }
        else
        {
            List<SslApplicationProtocol> matched =
            [
                negotiatedUpstreamProtocol,
            ];
            protocols = matched;
        }

        var options = new SslServerAuthenticationOptions
        {
            ApplicationProtocols = protocols,
            ClientCertificateRequired = false,
            ServerCertificate = leafCertificate,
        };
        return options;
    }

    /// <summary>
    ///     Builds a new <see cref="TrafficFlow" /> for an accepted connection. Falls back to the
    ///     literal "unknown" sentinel when the connection has no remote endpoint.
    /// </summary>
    /// <param name="connection">The proxy connection.</param>
    /// <returns>A new <see cref="TrafficFlow" /> in the Pending state.</returns>
    public static TrafficFlow CreateTrafficFlow(IProxyConnection connection)
    {
        var clientEndPoint = connection.RemoteEndPoint?.ToString();

        if (string.IsNullOrWhiteSpace(clientEndPoint))
        {
            clientEndPoint = "unknown";
        }

        var flow = new TrafficFlow(Guid.NewGuid(), clientEndPoint, DateTimeOffset.UtcNow);
        return flow;
    }

    /// <summary>
    ///     Determines whether the supplied header collection contains a Connection: close directive.
    /// </summary>
    /// <param name="headers">The headers to inspect.</param>
    /// <returns><see langword="true" /> when the directive is present.</returns>
    public static bool HasConnectionCloseDirective(HeaderCollection headers)
    {
        var connectionValue = headers.Get("Connection");

        if (string.IsNullOrWhiteSpace(connectionValue))
        {
            return false;
        }

        return connectionValue.Contains("close", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether the proxy can keep the client connection alive after sending the
    ///     response (HTTP/1.1 + Content-Length + no Connection: close).
    /// </summary>
    /// <param name="request">The captured request.</param>
    /// <param name="response">The captured response.</param>
    /// <returns><see langword="true" /> when keep-alive is appropriate.</returns>
    public static bool HasKeepAlive(
        HypertextTransferProtocolRequestData request,
        HypertextTransferProtocolResponseData response)
    {
        if (string.Equals(request.Version, "HTTP/1.0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!response.Headers.HasHeader("Content-Length"))
        {
            return false;
        }

        if (HasConnectionCloseDirective(request.Headers) || HasConnectionCloseDirective(response.Headers))
        {
            return false;
        }

        return true;
    }
}
