using System;
using System.Buffers;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helper that detects SOCKS protocol version from the first bytes of a connection
///     stream. SOCKS4 begins with version byte 0x04, SOCKS5 with 0x05. Anything else returns null.
/// </summary>
public static class SocksProtocolDetector
{
    /// <summary>
    ///     Inspects the initial bytes and returns the detected SOCKS version, or null when
    ///     not a SOCKS handshake.
    /// </summary>
    /// <param name="initialBytes">The first bytes received on the connection.</param>
    /// <returns>The detected version, or null.</returns>
    public static SocksVersion? Detect(ReadOnlySequence<byte> initialBytes)
    {
        if (initialBytes.Length == 0)
        {
            return null;
        }

        var first = ExtractFirstByte(initialBytes);

        if (first == 0x04)
        {
            return SocksVersion.Four;
        }

        if (first == 0x05)
        {
            return SocksVersion.Five;
        }

        return null;
    }

    private static byte ExtractFirstByte(ReadOnlySequence<byte> initialBytes)
    {
        Span<byte> buffer = stackalloc byte[1];
        initialBytes.Slice(0, 1).CopyTo(buffer);
        return buffer[0];
    }
}
