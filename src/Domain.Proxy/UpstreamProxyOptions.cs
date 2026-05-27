using System.Collections.Generic;

namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Configuration for forwarding outbound proxy traffic through an upstream HTTP/HTTPS proxy server.
///     When <see cref="IsEnabled" /> is <see langword="false" /> the proxy connects to origin servers directly.
/// </summary>
public sealed class UpstreamProxyOptions
{
    /// <summary>
    ///     Gets or sets the bypass-pattern list. Each pattern matches a destination host using simple
    ///     wildcards (<c>*</c>, <c>?</c>); destinations matching any pattern are connected to directly
    ///     instead of being forwarded through the upstream proxy.
    /// </summary>
    public IList<string> BypassPatterns { get; set; }

    /// <summary>
    ///     Gets or sets the upstream proxy host name (e.g., <c>"corp-proxy.example.com"</c>).
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether upstream-proxy forwarding is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the password component of the optional Basic credentials used to authenticate
    ///     with the upstream proxy.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    ///     Gets or sets the upstream proxy port. Default: 8080.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Gets or sets the username component of the optional Basic credentials used to authenticate
    ///     with the upstream proxy.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="UpstreamProxyOptions" /> with defaults (disabled, port 8080).
    /// </summary>
    public UpstreamProxyOptions()
    {
        IsEnabled = false;
        Port = 8080;
        List<string> bypassPatterns = [];
        BypassPatterns = bypassPatterns;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when both <see cref="Username" /> and <see cref="Password" />
    ///     are non-empty so that a Basic authentication header can be sent.
    /// </summary>
    /// <returns><see langword="true" /> when credentials are usable.</returns>
    public bool HasCredentials()
    {
        if (string.IsNullOrEmpty(Username))
        {
            return false;
        }

        if (Password is null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the options have an enabled state with a non-empty
    ///     host and a port in the legal range 1-65535.
    /// </summary>
    /// <returns><see langword="true" /> when the options can be used for forwarding.</returns>
    public bool HasValidConfiguration()
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            return false;
        }

        if (Port is < 1 or > 65535)
        {
            return false;
        }

        return true;
    }
}
