using System;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 DATA frame (RFC 7540 § 6.1), stripping the optional
///     padding wrapper when the <c>PADDED</c> flag is set:
///     <list type="bullet">
///       <item><description>Pad Length (1 octet, present iff PADDED).</description></item>
///       <item><description>Data (the application bytes; variable length).</description></item>
///       <item><description>Padding (Pad Length octets; opaque, not validated here).</description></item>
///     </list>
///     Returns <c>null</c> when the payload is too short to contain the pad-length octet, or
///     when the declared padding length exceeds the available payload — both are
///     FRAME_SIZE_ERROR cases per the specification.
/// </summary>
public static class HypertextTransferProtocolVersion2DataFramePayloadParser
{
    /// <summary>
    ///     Returns the application-data view of <paramref name="payload" />, with the optional
    ///     padding stripped when <paramref name="hasPaddedFlag" /> is set.
    /// </summary>
    /// <param name="payload">The raw payload of the DATA frame.</param>
    /// <param name="hasPaddedFlag">Whether the PADDED flag was set on the frame header.</param>
    /// <returns>The application bytes on success; <c>null</c> when the payload is malformed.</returns>
    public static ReadOnlyMemory<byte>? Parse(ReadOnlyMemory<byte> payload, bool hasPaddedFlag)
    {
        if (!hasPaddedFlag)
        {
            return payload;
        }
        if (payload.Length < 1)
        {
            return null;
        }
        var paddingLength = payload.Span[0];
        var dataStart = 1;
        var dataEnd = payload.Length - paddingLength;
        if (dataEnd < dataStart)
        {
            return null;
        }
        var data = payload[dataStart..dataEnd];
        return data;
    }
}
