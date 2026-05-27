using Proxyfan.Domain.Proxy;
using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Builds the <c>Proxy-Authorization</c> header value for HTTP Basic credentials configured
///     on an <see cref="UpstreamProxyOptions" />. The resulting value is the literal
///     <c>"Basic "</c> followed by the Base64-encoded UTF-8 representation of
///     <c>"{username}:{password}"</c> as defined by RFC 7617 and RFC 7235 §4.4.
/// </summary>
public static class ProxyAuthorizationHeader
{
    /// <summary>
    ///     Returns the Basic credentials header value for the supplied <paramref name="options" />,
    ///     or <see langword="null" /> when no credentials are configured.
    /// </summary>
    /// <param name="options">The upstream proxy options carrying optional credentials.</param>
    /// <returns>The header value or <see langword="null" /> when credentials are absent.</returns>
    public static string? Build(UpstreamProxyOptions options)
    {
        if (!options.HasCredentials())
        {
            return null;
        }

        var raw = $"{options.Username}:{options.Password}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        var encoded = Convert.ToBase64String(bytes);
        return $"Basic {encoded}";
    }
}
