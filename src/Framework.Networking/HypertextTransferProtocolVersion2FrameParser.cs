using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parser for HTTP/2 frames (RFC 7540 § 4.1). The parser is stateless and pure — callers
///     buffer transport bytes themselves and ask for either just the header or the full frame
///     including payload.
/// </summary>
public static class HypertextTransferProtocolVersion2FrameParser
{
    /// <summary>
    ///     Length of the fixed frame header (9 octets: 3 length + 1 type + 1 flags + 4 stream id).
    /// </summary>
    public const int HeaderLength = 9;

    /// <summary>
    ///     Tries to parse a complete frame (header + payload) from the supplied buffer. Returns
    ///     <see langword="null" /> when the buffer is shorter than the declared total length.
    /// </summary>
    /// <param name="buffer">The buffer to parse from.</param>
    /// <returns>The parsed frame, or null when more bytes are needed.</returns>
    public static HypertextTransferProtocolVersion2Frame? TryParse(ReadOnlyMemory<byte> buffer)
    {
        var header = TryParseHeader(buffer.Span);
        if (header is null)
        {
            return null;
        }

        var totalLength = HeaderLength + header.Length;
        if (buffer.Length < totalLength)
        {
            return null;
        }

        var payload = buffer.Slice(HeaderLength, header.Length);
        var frame = new HypertextTransferProtocolVersion2Frame(header, payload);
        return frame;
    }

    /// <summary>
    ///     Tries to parse just the 9-octet frame header. Returns <see langword="null" /> when
    ///     fewer than 9 bytes are available. Unknown frame types are surfaced through
    ///     <see cref="HypertextTransferProtocolVersion2FrameHeader.RawType" /> and
    ///     <see cref="HypertextTransferProtocolVersion2FrameHeader.IsKnownType" /> per
    ///     RFC 7540 § 4.1 (implementations must skip over unknown frame types, not error).
    /// </summary>
    /// <param name="buffer">The buffer to parse from.</param>
    /// <returns>The parsed header, or null when more bytes are needed.</returns>
    public static HypertextTransferProtocolVersion2FrameHeader? TryParseHeader(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < HeaderLength)
        {
            return null;
        }

        var length = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
        var typeByte = buffer[3];
        var flagsByte = buffer[4];
        var streamId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(5, 4)) & 0x7FFFFFFFU;

        var flags = (HypertextTransferProtocolVersion2FrameFlag)flagsByte;
        var header = new HypertextTransferProtocolVersion2FrameHeader(length, typeByte, flags, streamId);
        return header;
    }
}
