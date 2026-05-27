namespace Proxyfan.Domain.Proxy;

/// <summary>
///     Configuration for the reverse proxy feature. When enabled, Proxyfan acts as a public-facing
///     proxy on <see cref="ListenPort" /> and forwards all received requests to
///     <see cref="UpstreamHost" />:<see cref="UpstreamPort" />.
/// </summary>
public sealed class ReverseProxyOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether to use TLS to the backend.
    /// </summary>
    public bool AllowTransportLayerSecurityToUpstream { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the reverse proxy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the TCP port the reverse proxy listens on for incoming public connections.
    /// </summary>
    public int ListenPort { get; set; }

    /// <summary>
    ///     Gets or sets the backend host name that requests are forwarded to.
    /// </summary>
    public string? UpstreamHost { get; set; }

    /// <summary>
    ///     Gets or sets the backend port that requests are forwarded to.
    /// </summary>
    public int UpstreamPort { get; set; }

    /// <summary>
    ///     Initializes a new <see cref="ReverseProxyOptions" /> with defaults (disabled, listen 8888,
    ///     upstream port 80, no TLS).
    /// </summary>
    public ReverseProxyOptions()
    {
        IsEnabled = false;
        ListenPort = 8888;
        UpstreamPort = 80;
        AllowTransportLayerSecurityToUpstream = false;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the configuration is enabled and has a valid
    ///     listen port, upstream host, and upstream port.
    /// </summary>
    /// <returns><see langword="true" /> when usable.</returns>
    public bool HasValidConfiguration()
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (ListenPort is < 1 or > 65535)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(UpstreamHost))
        {
            return false;
        }

        if (UpstreamPort is < 1 or > 65535)
        {
            return false;
        }

        return true;
    }
}
