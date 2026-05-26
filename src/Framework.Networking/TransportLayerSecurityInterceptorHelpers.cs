using Proxyfan.Domain.Proxy;
using Proxyfan.Domain.Traffic;
using System;
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
    ///     Builds an <see cref="SslClientAuthenticationOptions" /> for use when the proxy connects
    ///     to the upstream server as a TLS client.
    /// </summary>
    /// <param name="target">The CONNECT target whose host is used for SNI.</param>
    /// <returns>A populated <see cref="SslClientAuthenticationOptions" />.</returns>
    public static SslClientAuthenticationOptions CreateClientTransportLayerSecurityOptions(ConnectTarget target)
    {
        var options = new SslClientAuthenticationOptions
        {
            TargetHost = target.Host,
        };
        return options;
    }

    /// <summary>
    ///     Builds an <see cref="SslServerAuthenticationOptions" /> for use when the proxy presents
    ///     the per-host leaf certificate to the intercepted client.
    /// </summary>
    /// <param name="leafCertificate">The leaf certificate signed by the proxy CA.</param>
    /// <returns>A populated <see cref="SslServerAuthenticationOptions" />.</returns>
    public static SslServerAuthenticationOptions CreateServerTransportLayerSecurityOptions(X509Certificate2 leafCertificate)
    {
        var options = new SslServerAuthenticationOptions
        {
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
