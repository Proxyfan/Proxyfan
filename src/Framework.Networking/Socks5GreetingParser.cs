using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static parser for SOCKS5 client greeting messages (RFC 1928 § 3). The parser is pure
///     and returns null when the supplied buffer does not yet contain a complete greeting.
/// </summary>
public static class Socks5GreetingParser
{
    /// <summary>
    ///     Attempts to parse a SOCKS5 client greeting from the supplied buffer.
    /// </summary>
    /// <param name="buffer">The source bytes.</param>
    /// <returns>The parsed greeting, or null when more bytes are required.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the version byte is not 0x05 or method count is zero.
    /// </exception>
    public static Socks5Greeting? TryParse(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;

        if (span.Length < 2)
        {
            return null;
        }

        if (span[0] != 0x05)
        {
            throw new System.IO.InvalidDataException("First byte is not the SOCKS5 version (0x05).");
        }

        var methodCount = span[1];

        if (methodCount == 0)
        {
            throw new System.IO.InvalidDataException("SOCKS5 greeting must list at least one method.");
        }

        var totalLength = 2 + methodCount;

        if (span.Length < totalLength)
        {
            return null;
        }

        var methods = new byte[methodCount];
        span.Slice(2, methodCount).CopyTo(methods);
        var greeting = new Socks5Greeting(methods, totalLength);
        return greeting;
    }
}
