using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Writes the payload of an HTTP/2 SETTINGS frame (RFC 7540 § 6.5) by encoding a sequence
///     of <see cref="HypertextTransferProtocolVersion2SettingParameter" />s. Each parameter
///     occupies exactly six octets (2-byte identifier + 4-byte value).
/// </summary>
public static class HypertextTransferProtocolVersion2SettingsWriter
{
    private const int ParameterSize = 6;

    /// <summary>
    ///     Computes the number of bytes required to encode <paramref name="parameters" />.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <returns>The number of bytes required.</returns>
    public static int ComputeByteSize(IReadOnlyList<HypertextTransferProtocolVersion2SettingParameter> parameters)
    {
        return parameters.Count * ParameterSize;
    }

    /// <summary>
    ///     Writes the parameters into <paramref name="destination" /> as the body of a SETTINGS
    ///     frame.
    /// </summary>
    /// <param name="destination">Destination buffer.</param>
    /// <param name="parameters">Parameters to encode.</param>
    /// <returns>The number of bytes written.</returns>
    public static int Write(
        Span<byte> destination,
        IReadOnlyList<HypertextTransferProtocolVersion2SettingParameter> parameters)
    {
        var required = parameters.Count * ParameterSize;
        if (destination.Length < required)
        {
            throw new ArgumentException("Destination buffer is too small for the SETTINGS payload.", nameof(destination));
        }
        for (var index = 0; index < parameters.Count; index++)
        {
            var offset = index * ParameterSize;
            var parameter = parameters[index];
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(offset, 2), parameter.Identifier);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(offset + 2, 4), parameter.Value);
        }
        return required;
    }
}
