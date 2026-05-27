using System.Net;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parsed SOCKS4 CONNECT request (CD=1) per the original SOCKS4 specification.
/// </summary>
public sealed class Socks4ConnectRequest
{
    /// <summary>
    ///     Gets the destination IPv4 address.
    /// </summary>
    public IPAddress DestinationAddress { get; }

    /// <summary>
    ///     Gets the destination port.
    /// </summary>
    public int DestinationPort { get; }

    /// <summary>
    ///     Gets the USERID field value (often empty).
    /// </summary>
    public string UserId { get; }

    /// <summary>
    ///     Initializes a new <see cref="Socks4ConnectRequest" />.
    /// </summary>
    /// <param name="destinationAddress">The destination IPv4 address.</param>
    /// <param name="destinationPort">The destination port.</param>
    /// <param name="userId">The USERID field.</param>
    public Socks4ConnectRequest(IPAddress destinationAddress, int destinationPort, string userId)
    {
        DestinationAddress = destinationAddress;
        DestinationPort = destinationPort;
        UserId = userId;
    }
}
