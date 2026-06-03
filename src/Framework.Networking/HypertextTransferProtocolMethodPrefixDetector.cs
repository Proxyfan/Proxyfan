using System;
using System.Buffers;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Static helper that detects whether a buffered byte sequence begins with an HTTP/1.x
///     request-line method token (one or more characters drawn from uppercase ASCII or the
///     <c>-</c> byte, followed by a space). Used by the reverse proxy route listener to
///     decide whether to dispatch the connection to the HTTP-capture handler.
///
///     The accepted grammar intentionally tolerates extension method tokens permitted by
///     RFC 7230 §3.1.1 (restricted to uppercase ALPHA plus <c>-</c> to remain robust
///     against unrelated binary traffic), so WebDAV methods such as <c>PROPFIND</c> /
///     <c>MKCOL</c> and other application-specific extension methods are no longer
///     rejected before they reach the request parser. Hyphens are not constrained to
///     interior positions: leading or repeated <c>-</c> bytes are accepted at this
///     screening layer and the full request-line parser is the final authority on
///     well-formedness.
/// </summary>
public static class HypertextTransferProtocolMethodPrefixDetector
{
    /// <summary>
    ///     Upper bound on the number of bytes inspected when scanning for the method-token
    ///     terminating space. RFC 7230 does not bound the method length, but real HTTP method
    ///     tokens are short; capping the peek keeps the detector cheap and prevents arbitrary
    ///     binary payloads from causing the scan to run away looking for a space.
    /// </summary>
    private const int MaximumMethodTokenLength = 32;

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied bytes begin with a plausible
    ///     HTTP/1.x request-line method token (1..<see cref="MaximumMethodTokenLength" />
    ///     uppercase ASCII letters or <c>-</c>) followed by a space character.
    /// </summary>
    /// <param name="initialBytes">The buffered initial bytes from the connection.</param>
    /// <returns>True when the bytes look like the start of an HTTP request line.</returns>
    public static bool HasMethodPrefix(ReadOnlySequence<byte> initialBytes)
    {
        var peekLength = (int)Math.Min(initialBytes.Length, MaximumMethodTokenLength + 1);
        if (peekLength < 2)
        {
            return false;
        }

        Span<byte> peek = stackalloc byte[peekLength];
        initialBytes.Slice(0, peekLength).CopyTo(peek);

        for (var index = 0; index < peek.Length; index++)
        {
            var candidate = peek[index];
            if (candidate == (byte)' ')
            {
                return index > 0;
            }

            var isUppercaseAlpha = candidate is >= (byte)'A' and <= (byte)'Z';
            var isHyphen = candidate == (byte)'-';
            if (!isUppercaseAlpha && !isHyphen)
            {
                return false;
            }
        }

        return false;
    }
}
