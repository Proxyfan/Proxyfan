using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 PUSH_PROMISE frame (RFC 7540 § 6.6). The payload layout is:
///     <list type="bullet">
///       <item><description>Pad Length (1 octet, present iff PADDED flag is set).</description></item>
///       <item><description>Promised Stream Identifier (4 octets, top reserved bit ignored).</description></item>
///       <item><description>Header Block Fragment (variable length).</description></item>
///       <item><description>Padding (Pad Length octets, must be zeroed; not validated here).</description></item>
///     </list>
///     Returns <c>null</c> when the payload is too short to contain the mandatory fields or
///     when the padding length exceeds the available payload — both are FRAME_SIZE_ERROR cases.
/// </summary>
public static class HypertextTransferProtocolVersion2PushPromiseParser
{
    /// <summary>
    ///     Parses <paramref name="payload" /> into a <see cref="HypertextTransferProtocolVersion2PushPromise" />.
    /// </summary>
    /// <param name="payload">The raw payload of the PUSH_PROMISE frame.</param>
    /// <param name="hasPaddedFlag">Whether the PADDED flag was set on the frame header.</param>
    /// <returns>The parsed push-promise on success; <c>null</c> when the payload is malformed.</returns>
    public static HypertextTransferProtocolVersion2PushPromise? Parse(ReadOnlyMemory<byte> payload, bool hasPaddedFlag)
    {
        var span = payload.Span;
        var offset = 0;
        var paddingLength = 0;
        if (hasPaddedFlag)
        {
            if (span.Length < 1)
            {
                return null;
            }
            paddingLength = span[0];
            offset = 1;
        }
        if (span.Length < offset + 4)
        {
            return null;
        }
        var raw = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
        var promisedStreamIdentifier = raw & 0x7FFFFFFFu;
        offset += 4;
        var fragmentEnd = span.Length - paddingLength;
        if (fragmentEnd < offset)
        {
            return null;
        }
        var fragment = payload[offset..fragmentEnd];
        return new HypertextTransferProtocolVersion2PushPromise(promisedStreamIdentifier, fragment);
    }
}
