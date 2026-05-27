namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parsed SOCKS5 CONNECT request (RFC 1928 § 4): destination address (IPv4, IPv6, or
///     domain name) and destination port.
/// </summary>
public sealed class Socks5ConnectRequest
{
    /// <summary>
    ///     Gets the destination address as a string. For IP types this is the formatted IP;
    ///     for <see cref="Socks5AddressType.DomainName" /> this is the ASCII hostname.
    /// </summary>
    public string DestinationAddress { get; }

    /// <summary>
    ///     Gets the address type of the destination.
    /// </summary>
    public Socks5AddressType DestinationAddressType { get; }

    /// <summary>
    ///     Gets the destination port.
    /// </summary>
    public int DestinationPort { get; }

    /// <summary>
    ///     Gets the total number of bytes the request consumed from the wire.
    /// </summary>
    public int TotalLength { get; }

    /// <summary>
    ///     Initializes a new <see cref="Socks5ConnectRequest" />.
    /// </summary>
    /// <param name="destinationAddressType">The address type.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="destinationPort">The destination port.</param>
    /// <param name="totalLength">The total wire-format byte count.</param>
    public Socks5ConnectRequest(Socks5AddressType destinationAddressType, string destinationAddress, int destinationPort, int totalLength)
    {
        DestinationAddressType = destinationAddressType;
        DestinationAddress = destinationAddress;
        DestinationPort = destinationPort;
        TotalLength = totalLength;
    }
}
