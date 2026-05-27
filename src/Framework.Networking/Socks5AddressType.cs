namespace Proxyfan.Framework.Networking;

/// <summary>
///     Address type byte from a SOCKS5 CONNECT request (RFC 1928 § 4 ATYP field).
/// </summary>
public enum Socks5AddressType
{
    /// <summary>
    ///     IPv4 address (4 bytes).
    /// </summary>
    InternetProtocolVersionFour = 1,

    /// <summary>
    ///     Domain name (1-byte length prefix + N bytes ASCII).
    /// </summary>
    DomainName = 3,

    /// <summary>
    ///     IPv6 address (16 bytes).
    /// </summary>
    InternetProtocolVersionSix = 4,
}
