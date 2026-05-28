using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 HEADERS frame (RFC 7540 § 6.2) into the bare header
///     block fragment that downstream consumers feed into the HPACK decoder. The HEADERS
///     payload layout is:
///     <list type="bullet">
///       <item><description>Pad Length (1 octet, present iff PADDED).</description></item>
///       <item><description>Priority dependency block (5 octets — 4-byte stream dependency + 1-byte weight, present iff PRIORITY).</description></item>
///       <item><description>Header Block Fragment (variable length).</description></item>
///       <item><description>Padding (Pad Length octets; opaque, not validated here).</description></item>
///     </list>
///     The PRIORITY block is parsed but discarded: RFC 9113 deprecated stream-level priority
///     and the proxy does not multiplex by priority. Returns <c>null</c> when the payload is
///     malformed (too short for declared flags, or padding exceeds available bytes) — both
///     are FRAME_SIZE_ERROR cases.
/// </summary>
public static class HypertextTransferProtocolVersion2HeadersFramePayloadParser
{
    private const int PriorityBlockSize = 5;

    /// <summary>
    ///     Returns the header-block-fragment view of <paramref name="payload" /> with optional
    ///     padding and PRIORITY block stripped.
    /// </summary>
    /// <param name="payload">The raw payload of the HEADERS frame.</param>
    /// <param name="hasPaddedFlag">Whether the PADDED flag was set on the frame header.</param>
    /// <param name="hasPriorityFlag">Whether the PRIORITY flag was set on the frame header.</param>
    /// <returns>The header block fragment on success; <c>null</c> when the payload is malformed.</returns>
    public static ReadOnlyMemory<byte>? Parse(ReadOnlyMemory<byte> payload, bool hasPaddedFlag, bool hasPriorityFlag)
    {
        var offset = 0;
        var paddingLength = 0;
        if (hasPaddedFlag)
        {
            if (payload.Length < 1)
            {
                return null;
            }
            paddingLength = payload.Span[0];
            offset = 1;
        }
        if (hasPriorityFlag)
        {
            if (payload.Length < offset + PriorityBlockSize)
            {
                return null;
            }
            offset += PriorityBlockSize;
        }
        var fragmentEnd = payload.Length - paddingLength;
        if (fragmentEnd < offset)
        {
            return null;
        }
        var fragment = payload[offset..fragmentEnd];
        return fragment;
    }
}
