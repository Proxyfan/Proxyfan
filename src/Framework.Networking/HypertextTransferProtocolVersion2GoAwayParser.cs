using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 GOAWAY frame (RFC 7540 § 6.8). Payload layout:
///     <list type="bullet">
///       <item><description>Last-Stream-ID (4 octets, top reserved bit ignored).</description></item>
///       <item><description>Error Code (4 octets).</description></item>
///       <item><description>Additional Debug Data (variable length, may be empty).</description></item>
///     </list>
///     Returns <c>null</c> when the payload is shorter than the 8-octet mandatory prefix.
/// </summary>
public static class HypertextTransferProtocolVersion2GoAwayParser
{
    /// <summary>
    ///     Parses <paramref name="payload" /> into a <see cref="HypertextTransferProtocolVersion2GoAway" />.
    /// </summary>
    /// <param name="payload">The raw payload of the GOAWAY frame.</param>
    /// <returns>The parsed GOAWAY on success; <c>null</c> when the payload is malformed.</returns>
    public static HypertextTransferProtocolVersion2GoAway? Parse(ReadOnlyMemory<byte> payload)
    {
        if (payload.Length < 8)
        {
            return null;
        }
        var span = payload.Span;
        var rawLastStreamIdentifier = BinaryPrimitives.ReadUInt32BigEndian(span[..4]);
        var lastStreamIdentifier = rawLastStreamIdentifier & 0x7FFFFFFFu;
        var errorCode = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4, 4));
        var debugData = payload[8..];
        return new HypertextTransferProtocolVersion2GoAway(lastStreamIdentifier, errorCode, debugData);
    }
}
