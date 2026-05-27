using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Writer for HTTP/2 frame headers and complete frames (RFC 7540 § 4.1). The writer is
///     stateless and pure — callers supply a destination span and receive the number of bytes
///     written. It is used by the proxy's HTTP/2 connection handler to emit SETTINGS, PING ACK,
///     WINDOW_UPDATE, GOAWAY, HEADERS, DATA, and RST_STREAM frames.
/// </summary>
public static class HypertextTransferProtocolVersion2FrameWriter
{
    /// <summary>
    ///     Writes a complete frame (header + payload) into <paramref name="destination" /> and
    ///     returns the total number of bytes written.
    /// </summary>
    /// <param name="destination">Destination buffer to write into.</param>
    /// <param name="descriptor">Frame header fields (the payload length must match the supplied payload).</param>
    /// <param name="payload">Payload bytes to copy after the header.</param>
    /// <returns>The total number of bytes written (9 + payload length).</returns>
    public static int WriteFrame(
        Span<byte> destination,
        HypertextTransferProtocolVersion2FrameDescriptor descriptor,
        ReadOnlySpan<byte> payload)
    {
        var totalLength = HypertextTransferProtocolVersion2FrameParser.HeaderLength + payload.Length;
        if (destination.Length < totalLength)
        {
            throw new ArgumentException("Destination buffer is too small to hold the frame.", nameof(destination));
        }
        WriteHeader(destination, descriptor);
        payload.CopyTo(destination[HypertextTransferProtocolVersion2FrameParser.HeaderLength..]);
        return totalLength;
    }

    /// <summary>
    ///     Writes a 9-octet frame header into <paramref name="destination" /> and returns the
    ///     number of bytes written.
    /// </summary>
    /// <param name="destination">Destination buffer to write into.</param>
    /// <param name="descriptor">Frame header fields.</param>
    /// <returns>The number of bytes written (always 9 on success).</returns>
    public static int WriteHeader(Span<byte> destination, HypertextTransferProtocolVersion2FrameDescriptor descriptor)
    {
        var payloadLength = descriptor.PayloadLength;
        if (payloadLength is < 0 or > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(descriptor), payloadLength, "Payload length must be in [0, 16777215].");
        }
        if (destination.Length < HypertextTransferProtocolVersion2FrameParser.HeaderLength)
        {
            throw new ArgumentException("Destination buffer is too small to hold a frame header.", nameof(destination));
        }
        destination[0] = (byte)((payloadLength >> 16) & 0xFF);
        destination[1] = (byte)((payloadLength >> 8) & 0xFF);
        destination[2] = (byte)(payloadLength & 0xFF);
        destination[3] = (byte)descriptor.Type;
        destination[4] = (byte)descriptor.Flags;
        var maskedStreamIdentifier = descriptor.StreamIdentifier & 0x7FFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(5, 4), maskedStreamIdentifier);
        return HypertextTransferProtocolVersion2FrameParser.HeaderLength;
    }
}
