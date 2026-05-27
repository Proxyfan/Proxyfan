namespace Proxyfan.Domain.RemoteDevices;

/// <summary>
///     Helpers for working with client endpoint strings in the format <c>host:port</c>.
/// </summary>
public static class ClientEndPointAddress
{
    /// <summary>
    ///     Returns the host portion of an endpoint string such as <c>10.0.0.1:54321</c>.
    /// </summary>
    /// <param name="clientEndPoint">The endpoint string.</param>
    /// <returns>The host (without the port), or empty when the input is empty.</returns>
    public static string Extract(string clientEndPoint)
    {
        if (string.IsNullOrEmpty(clientEndPoint))
        {
            return string.Empty;
        }

        var lastColon = clientEndPoint.LastIndexOf(':');
        if (lastColon <= 0)
        {
            return clientEndPoint;
        }

        return clientEndPoint[..lastColon];
    }
}
