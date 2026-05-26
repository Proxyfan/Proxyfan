using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Provides parsing of the first line of an HTTP CONNECT request to extract the
///     tunnel target host and port. The input is the raw header bytes read from the
///     client connection up to and including the blank line that terminates the headers.
/// </summary>
public static class ConnectRequestParser
{
    private const int DefaultTunnelPort = 443;

    /// <summary>
    ///     Parses the raw HTTP CONNECT request headers and extracts the tunnel target.
    /// </summary>
    /// <param name="headerBytes">
    ///     The raw bytes of the CONNECT request, from the first byte through the blank
    ///     line (<c>\r\n\r\n</c>) that ends the headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ConnectTarget" /> representing the destination host and port, or
    ///     <see langword="null" /> if the request could not be parsed.
    /// </returns>
    public static ConnectTarget? Parse(byte[] headerBytes)
    {
        var text = Encoding.ASCII.GetString(headerBytes);
        var requestLineEndIndex = text.IndexOf("\r\n", StringComparison.Ordinal);

        if (requestLineEndIndex < 0)
        {
            return null;
        }

        var requestLine = text.AsSpan(0, requestLineEndIndex);
        return ParseRequestLine(requestLine);
    }

    private static ConnectTarget? ParseAuthority(ReadOnlySpan<char> authority)
    {
        var colonIndex = authority.LastIndexOf(':');

        if (colonIndex < 0)
        {
            var host = authority.ToString();
            return new ConnectTarget(host, DefaultTunnelPort);
        }

        var hostSpan = authority[..colonIndex];
        var portSpan = authority[(colonIndex + 1)..];

        if (!int.TryParse(portSpan, out var port))
        {
            return null;
        }

        if (port is < 1 or > 65535)
        {
            return null;
        }

        var hostString = hostSpan.ToString();
        return new ConnectTarget(hostString, port);
    }

    private static ConnectTarget? ParseRequestLine(ReadOnlySpan<char> requestLine)
    {
        var spaceIndex = requestLine.IndexOf(' ');

        if (spaceIndex < 0)
        {
            return null;
        }

        var afterMethod = requestLine[(spaceIndex + 1)..];
        var nextSpaceIndex = afterMethod.IndexOf(' ');

        if (nextSpaceIndex < 0)
        {
            return null;
        }

        var authority = afterMethod[..nextSpaceIndex];
        return ParseAuthority(authority);
    }
}
