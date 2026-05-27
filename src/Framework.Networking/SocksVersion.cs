namespace Proxyfan.Framework.Networking;

/// <summary>
///     SOCKS protocol versions recognized by the dispatcher (RFC 1928 SOCKS5,
///     classic SOCKS4).
/// </summary>
public enum SocksVersion
{
    /// <summary>
    ///     SOCKS4 protocol (first byte 0x04).
    /// </summary>
    Four = 4,

    /// <summary>
    ///     SOCKS5 protocol (first byte 0x05).
    /// </summary>
    Five = 5,
}
