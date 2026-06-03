using System.Globalization;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the value of an HTTP <c>Host</c> header into a <see cref="ConnectTarget" />.
///     Supports plain hostnames, IPv4 literals, and bracketed IPv6 literals
///     (<c>[2001:db8::1]</c> or <c>[2001:db8::1]:8080</c>) per RFC 7230 §5.4 and
///     RFC 3986 §3.2.2. Lives in its own static class to satisfy the
///     static-in-non-static-class rule (ATXCS011).
/// </summary>
public static class HostHeaderEndpointParser
{
    /// <summary>
    ///     Parses <paramref name="hostValue" /> into a <see cref="ConnectTarget" />, returning
    ///     <c>null</c> when the value is malformed. Brackets surrounding an IPv6 literal are
    ///     stripped so the resulting <see cref="ConnectTarget.Host" /> is suitable for direct
    ///     use with <see cref="System.Net.Sockets.Socket" /> APIs.
    /// </summary>
    /// <param name="hostValue">The raw Host header value (must be non-null/whitespace).</param>
    /// <param name="defaultPort">The port to use when the Host header omits a port.</param>
    /// <returns>The parsed endpoint, or <c>null</c> when parsing fails.</returns>
    public static ConnectTarget? Parse(string hostValue, int defaultPort)
    {
        var trimmedHostValue = hostValue.Trim();

        if (trimmedHostValue.Length == 0)
        {
            return null;
        }

        if (trimmedHostValue.StartsWith('['))
        {
            return ParseBracketedIpv6(trimmedHostValue, defaultPort);
        }

        return ParseHostAndOptionalPort(trimmedHostValue, defaultPort);
    }

    private static ConnectTarget? ParseBracketedIpv6(string trimmedHostValue, int defaultPort)
    {
        var closeBracketIndex = trimmedHostValue.IndexOf(']');

        if (closeBracketIndex < 0)
        {
            return null;
        }

        var ipv6Host = trimmedHostValue[1..closeBracketIndex];

        if (string.IsNullOrWhiteSpace(ipv6Host))
        {
            return null;
        }

        var remainder = trimmedHostValue[(closeBracketIndex + 1)..];

        if (remainder.Length == 0)
        {
            return new ConnectTarget(ipv6Host, defaultPort);
        }

        if (remainder[0] != ':')
        {
            return null;
        }

        return TryBuildTarget(ipv6Host, remainder[1..].Trim());
    }

    private static ConnectTarget? ParseHostAndOptionalPort(string trimmedHostValue, int defaultPort)
    {
        var separatorIndex = trimmedHostValue.LastIndexOf(':');

        if (separatorIndex < 0)
        {
            return new ConnectTarget(trimmedHostValue, defaultPort);
        }

        var host = trimmedHostValue[..separatorIndex].Trim();
        var portText = trimmedHostValue[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        return TryBuildTarget(host, portText);
    }

    private static ConnectTarget? TryBuildTarget(string host, string portText)
    {
        if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            || port is < 1 or > 65535)
        {
            return null;
        }

        return new ConnectTarget(host, port);
    }
}
