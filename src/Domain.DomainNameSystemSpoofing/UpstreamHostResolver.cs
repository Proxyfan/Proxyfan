using System;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     Resolves an outbound connection hostname by consulting the configured
///     <see cref="DomainNameSystemOverrideMap" />. When an override exists for the supplied
///     hostname, the matching IP address (rendered as a string) is returned and used for the
///     subsequent TCP connect; otherwise the original hostname is returned unchanged so the
///     system DNS resolver handles it. This is the single integration point between the DNS
///     spoofing UI and the outbound connection path. All handlers that establish a TCP
///     connection to an upstream origin consult this resolver before dialing so that user-
///     configured DNS overrides take effect end-to-end (HTTP/1.1 forwarding, HTTPS CONNECT
///     tunnels, intercepted TLS upstreams, SOCKS tunnels, and HTTP Upgrade orchestration).
/// </summary>
public sealed class UpstreamHostResolver
{
    private readonly DomainNameSystemOverrideMap _overrideMap;

    /// <summary>
    ///     Initializes a new <see cref="UpstreamHostResolver" /> backed by the supplied
    ///     override map.
    /// </summary>
    /// <param name="overrideMap">The DNS override map to consult.</param>
    public UpstreamHostResolver(DomainNameSystemOverrideMap overrideMap)
    {
        _overrideMap = overrideMap;
    }

    /// <summary>
    ///     Returns the effective host string that an outbound TCP <c>ConnectAsync</c> call
    ///     should dial when targeting the supplied hostname. If a DNS override is configured
    ///     for the hostname, the override IP address (rendered via <see cref="object.ToString" />)
    ///     is returned. Otherwise the original hostname is returned verbatim so the operating
    ///     system DNS resolver handles resolution.
    /// </summary>
    /// <param name="hostname">
    ///     The hostname that the caller intends to dial. Numeric IP literals are also accepted
    ///     and pass through unchanged (the lookup will not match).
    /// </param>
    /// <returns>The effective host string to pass to <c>TcpClient.ConnectAsync</c>.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="hostname" /> is empty or whitespace.
    /// </exception>
    public string Resolve(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var address = _overrideMap.Resolve(hostname);
        if (address is null)
        {
            return hostname;
        }

        return address.ToString();
    }
}
