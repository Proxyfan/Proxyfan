using System;
using System.Net;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static parser for SOCKS4 CONNECT request bytes.
/// </summary>
public static class Socks4ConnectRequestParser
{
    /// <summary>
    ///     Attempts to parse a SOCKS4 CONNECT request from the supplied buffer. Returns null
    ///     when the buffer is too small or the request is not yet complete (USERID must be
    ///     terminated by a NUL byte).
    /// </summary>
    /// <param name="buffer">The source bytes.</param>
    /// <returns>The parsed request, or null.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the version byte is not 0x04 or the command byte is not 0x01 (CONNECT).
    /// </exception>
    public static Socks4ConnectRequest? TryParse(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;

        if (span.Length < 8)
        {
            return null;
        }

        if (span[0] != 0x04)
        {
            throw new System.IO.InvalidDataException("First byte is not the SOCKS4 version (0x04).");
        }

        if (span[1] != 0x01)
        {
            throw new System.IO.InvalidDataException("Only the SOCKS4 CONNECT command (0x01) is supported.");
        }

        var port = (span[2] << 8) | span[3];
        var addressBytes = new byte[]
        {
            span[4],
            span[5],
            span[6],
            span[7],
        };
        var address = new IPAddress(addressBytes);

        var userIdEnd = 8;
        while (userIdEnd < span.Length && span[userIdEnd] != 0x00)
        {
            userIdEnd++;
        }

        if (userIdEnd >= span.Length)
        {
            return null;
        }

        var userId = userIdEnd > 8 ? Encoding.ASCII.GetString(span[8..userIdEnd]) : string.Empty;
        var request = new Socks4ConnectRequest(address, port, userId);
        return request;
    }
}
