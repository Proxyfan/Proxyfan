using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parses the payload of an HTTP/2 SETTINGS frame (RFC 7540 § 6.5) into a sequence of
///     <see cref="HypertextTransferProtocolVersion2SettingParameter" />s. Each parameter
///     occupies exactly six octets (2-byte identifier + 4-byte value).
/// </summary>
public static class HypertextTransferProtocolVersion2SettingsParser
{
    private const int ParameterSize = 6;

    /// <summary>
    ///     Parses <paramref name="payload" /> into a list of SETTINGS parameters. Returns
    ///     <c>null</c> when <paramref name="payload" /> is not a multiple of 6 octets (a
    ///     FRAME_SIZE_ERROR per the specification).
    /// </summary>
    /// <param name="payload">The raw payload bytes of the SETTINGS frame.</param>
    /// <returns>The parsed parameters on success; <c>null</c> when the payload size is invalid.</returns>
    public static IReadOnlyList<HypertextTransferProtocolVersion2SettingParameter>? Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length % ParameterSize != 0)
        {
            return null;
        }
        var count = payload.Length / ParameterSize;
        var result = new List<HypertextTransferProtocolVersion2SettingParameter>(count);
        for (var index = 0; index < count; index++)
        {
            var offset = index * ParameterSize;
            var identifier = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(offset, 2));
            var value = BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(offset + 2, 4));
            var parameter = new HypertextTransferProtocolVersion2SettingParameter(identifier, value);
            result.Add(parameter);
        }
        return result;
    }
}
