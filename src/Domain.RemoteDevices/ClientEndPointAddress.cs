namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Helpers for working with client endpoint strings such as <c>host:port</c>,
///     <c>[ipv6]:port</c>, or bare hosts/IPs.
/// </summary>
public static class ClientEndPointAddress
{
    /// <summary>
    ///     Returns the host portion of an endpoint string. Supports IPv4 (<c>10.0.0.1:54321</c>),
    ///     hostnames (<c>example.com:443</c>), bracketed IPv6 (<c>[::1]:54321</c>), and bare IPv6
    ///     literals (<c>::1</c>, <c>2001:db8::1</c>).
    /// </summary>
    /// <param name="clientEndPoint">The endpoint string.</param>
    /// <returns>The host (without the port), or empty when the input is empty.</returns>
    public static string Extract(string clientEndPoint)
    {
        if (string.IsNullOrEmpty(clientEndPoint))
        {
            return string.Empty;
        }

        if (clientEndPoint[0] == '[')
        {
            var closingBracket = clientEndPoint.IndexOf(']');
            if (closingBracket > 1)
            {
                return clientEndPoint[1..closingBracket];
            }

            return clientEndPoint;
        }

        var firstColon = clientEndPoint.IndexOf(':');
        if (firstColon <= 0)
        {
            return clientEndPoint;
        }

        var lastColon = clientEndPoint.LastIndexOf(':');
        if (firstColon != lastColon)
        {
            return clientEndPoint;
        }

        return clientEndPoint[..lastColon];
    }
}
