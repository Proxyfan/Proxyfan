using System;
using System.Net;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static parser for SOCKS5 CONNECT request bytes (RFC 1928 § 4). Returns null when the
///     buffer does not yet contain a complete request. Throws for invalid or unsupported
///     command and address types.
/// </summary>
public static class Socks5ConnectRequestParser
{
    /// <summary>
    ///     Attempts to parse a SOCKS5 CONNECT request from the supplied buffer.
    /// </summary>
    /// <param name="buffer">The source bytes.</param>
    /// <returns>The parsed request, or null when more bytes are required.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the version or command byte is invalid, or the address type is unknown.
    /// </exception>
    public static Socks5ConnectRequest? TryParse(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;

        if (span.Length < 4)
        {
            return null;
        }

        if (span[0] != 0x05)
        {
            throw new System.IO.InvalidDataException("First byte is not the SOCKS5 version (0x05).");
        }

        if (span[1] != 0x01)
        {
            throw new System.IO.InvalidDataException("Only SOCKS5 CONNECT (0x01) is supported.");
        }

        var addressType = (Socks5AddressType)span[3];
        return addressType switch
        {
            Socks5AddressType.InternetProtocolVersionFour => ParseInternetProtocolVersionFour(span),
            Socks5AddressType.DomainName => ParseDomainName(span),
            Socks5AddressType.InternetProtocolVersionSix => ParseInternetProtocolVersionSix(span),
            _ => throw new System.IO.InvalidDataException($"Unknown SOCKS5 address type: 0x{span[3]:X2}"),
        };
    }

    private static Socks5ConnectRequest? ParseDomainName(ReadOnlySpan<byte> span)
    {
        if (span.Length < 5)
        {
            return null;
        }

        var nameLength = span[4];
        var totalLength = 5 + nameLength + 2;

        if (span.Length < totalLength)
        {
            return null;
        }

        var hostname = Encoding.ASCII.GetString(span.Slice(5, nameLength));
        var port = (span[5 + nameLength] << 8) | span[5 + nameLength + 1];
        var request = new Socks5ConnectRequest(Socks5AddressType.DomainName, hostname, port, totalLength);
        return request;
    }

    private static Socks5ConnectRequest? ParseInternetProtocolVersionFour(ReadOnlySpan<byte> span)
    {
        if (span.Length < 10)
        {
            return null;
        }

        var addressBytes = new byte[]
        {
            span[4],
            span[5],
            span[6],
            span[7],
        };
        var address = new IPAddress(addressBytes);
        var port = (span[8] << 8) | span[9];
        var request = new Socks5ConnectRequest(Socks5AddressType.InternetProtocolVersionFour, address.ToString(), port, 10);
        return request;
    }

    private static Socks5ConnectRequest? ParseInternetProtocolVersionSix(ReadOnlySpan<byte> span)
    {
        if (span.Length < 22)
        {
            return null;
        }

        var addressBytes = span.Slice(4, 16).ToArray();
        var address = new IPAddress(addressBytes);
        var port = (span[20] << 8) | span[21];
        var request = new Socks5ConnectRequest(Socks5AddressType.InternetProtocolVersionSix, address.ToString(), port, 22);
        return request;
    }
}
