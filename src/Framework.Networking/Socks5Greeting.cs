using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parsed SOCKS5 client greeting (RFC 1928 § 3): version byte (5), method count, and the
///     list of acceptable authentication methods proposed by the client.
/// </summary>
public sealed class Socks5Greeting
{
    /// <summary>
    ///     Gets the method numbers (e.g. 0x00 NoAuth, 0x02 Username/Password) the client supports.
    /// </summary>
    public IReadOnlyList<byte> Methods { get; }

    /// <summary>
    ///     Gets the total bytes consumed by this greeting.
    /// </summary>
    public int TotalLength { get; }

    /// <summary>
    ///     Initializes a new <see cref="Socks5Greeting" />.
    /// </summary>
    /// <param name="methods">The supported authentication methods.</param>
    /// <param name="totalLength">The total bytes consumed.</param>
    public Socks5Greeting(IReadOnlyList<byte> methods, int totalLength)
    {
        Methods = methods;
        TotalLength = totalLength;
    }
}
