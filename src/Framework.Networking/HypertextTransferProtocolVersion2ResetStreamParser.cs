using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 RST_STREAM frame (RFC 7540 § 6.4). The payload is
///     exactly four octets carrying a 32-bit error code; any other length is FRAME_SIZE_ERROR.
/// </summary>
public static class HypertextTransferProtocolVersion2ResetStreamParser
{
    /// <summary>
    ///     Parses <paramref name="payload" /> into an error code.
    /// </summary>
    /// <param name="payload">The raw payload of the RST_STREAM frame.</param>
    /// <returns>The 32-bit error code on success; <c>null</c> when the payload is malformed.</returns>
    public static uint? Parse(ReadOnlyMemory<byte> payload)
    {
        if (payload.Length != 4)
        {
            return null;
        }
        return BinaryPrimitives.ReadUInt32BigEndian(payload.Span);
    }
}
