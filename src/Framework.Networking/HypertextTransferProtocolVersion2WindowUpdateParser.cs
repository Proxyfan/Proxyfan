using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the 4-octet payload of an HTTP/2 WINDOW_UPDATE frame (RFC 7540 § 6.9) into a
///     31-bit unsigned increment value. The top bit (R) is reserved and must be ignored.
/// </summary>
public static class HypertextTransferProtocolVersion2WindowUpdateParser
{
    /// <summary>
    ///     Parses <paramref name="payload" /> and returns the window-size increment, or
    ///     <c>null</c> when the payload is not exactly 4 octets long or the increment is
    ///     zero (an illegal value per RFC 7540 § 6.9).
    /// </summary>
    /// <param name="payload">The 4-octet WINDOW_UPDATE payload.</param>
    /// <returns>The 31-bit increment on success; <c>null</c> otherwise.</returns>
    public static int? Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 4)
        {
            return null;
        }
        var raw = BinaryPrimitives.ReadUInt32BigEndian(payload);
        var increment = (int)(raw & 0x7FFFFFFF);
        if (increment == 0)
        {
            return null;
        }
        return increment;
    }
}
