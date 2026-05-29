using System;
using System.Buffers;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helper that detects whether a buffered byte sequence begins with an HTTP/1.1
///     request method token (GET, POST, etc. followed by a space). Used by both the forward
///     proxy connection dispatcher and the reverse proxy route listener to decide whether to
///     dispatch the connection to the HTTP-capture handler.
/// </summary>
public static class HypertextTransferProtocolMethodPrefixDetector
{
    private static readonly byte[][] MethodPrefixes;

    static HypertextTransferProtocolMethodPrefixDetector()
    {
        var methodPrefixes = new byte[][]
        {
            Encoding.ASCII.GetBytes("DELETE "),
            Encoding.ASCII.GetBytes("GET "),
            Encoding.ASCII.GetBytes("HEAD "),
            Encoding.ASCII.GetBytes("OPTIONS "),
            Encoding.ASCII.GetBytes("PATCH "),
            Encoding.ASCII.GetBytes("POST "),
            Encoding.ASCII.GetBytes("PUT "),
            Encoding.ASCII.GetBytes("TRACE "),
        };
        MethodPrefixes = methodPrefixes;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied bytes begin with any HTTP/1.1
    ///     method token followed by a space.
    /// </summary>
    /// <param name="initialBytes">The buffered initial bytes from the connection.</param>
    /// <returns>True when the bytes look like the start of an HTTP request line.</returns>
    public static bool HasMethodPrefix(ReadOnlySequence<byte> initialBytes)
    {
        foreach (var methodPrefix in MethodPrefixes)
        {
            if (HasStartWith(initialBytes, methodPrefix))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasStartWith(ReadOnlySequence<byte> initialBytes, byte[] prefix)
    {
        if (initialBytes.Length < prefix.Length)
        {
            return false;
        }

        Span<byte> candidatePrefix = stackalloc byte[prefix.Length];
        initialBytes.Slice(0, prefix.Length).CopyTo(candidatePrefix);
        return candidatePrefix.SequenceEqual(prefix);
    }
}
