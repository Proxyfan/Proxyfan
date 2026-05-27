namespace Proxyfan.Framework.Networking;

/// <summary>
///     Pure validator that decides whether a CONNECT target is acceptable for tunneling
///     (port in valid range, host non-empty, no obvious injection attempts).
///     Used by both <see cref="ConnectTunnelHandler" /> and
///     <see cref="TransportLayerSecurityInterceptorHandler" /> to reject malformed targets
///     before opening a TCP connection.
/// </summary>
public static class ConnectTargetValidator
{
    private const int MaximumPort = 65535;
    private const int MinimumPort = 1;

    /// <summary>
    ///     Returns true when the supplied host/port pair is acceptable for tunneling.
    /// </summary>
    /// <param name="host">The destination host.</param>
    /// <param name="port">The destination port.</param>
    /// <returns>True when the target is acceptable.</returns>
    public static bool HasValidTarget(string? host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (port is < MinimumPort or > MaximumPort)
        {
            return false;
        }

        if (host.Contains('\r') || host.Contains('\n'))
        {
            return false;
        }

        return true;
    }
}
