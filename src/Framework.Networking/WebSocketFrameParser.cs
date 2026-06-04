using Proxyfan.Domain.Traffic;
using System;
using System.Buffers.Binary;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     Parser for WebSocket frames in the RFC 6455 wire format. The parser is purely
///     functional — it does not buffer or own state beyond what the caller provides.
/// </summary>
public static class WebSocketFrameParser
{
    private const long MaxFramePayloadBytes = 1024L * 1024L;

    /// <summary>
    ///     Attempts to parse one frame from the supplied buffer. Returns null when the buffer
    ///     does not contain at least one complete frame (caller must wait for more bytes).
    /// </summary>
    /// <param name="buffer">The source byte buffer.</param>
    /// <returns>The parsed frame, or null when insufficient bytes are available.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the buffer contains a malformed frame (bad reserved bits, undefined
    ///     opcode, or oversized control frame).
    /// </exception>
    public static WebSocketFrame? TryParse(ReadOnlyMemory<byte> buffer)
    {
        var span = buffer.Span;

        if (span.Length < 2)
        {
            return null;
        }

        var firstByte = span[0];
        var isFinalFragment = (firstByte & 0x80) != 0;

        if ((firstByte & 0x70) != 0)
        {
            throw new System.IO.InvalidDataException(
                $"WebSocket frame has reserved bits set (RSV1/RSV2/RSV3): 0x{firstByte:X2}.");
        }

        var opcodeRaw = firstByte & 0x0F;

        if (!WebSocketOpcodes.HasKnownValue(opcodeRaw))
        {
            throw new System.IO.InvalidDataException($"Unknown WebSocket opcode: 0x{opcodeRaw:X}.");
        }

        var opcode = (WebSocketOpcode)opcodeRaw;
        var secondByte = span[1];
        var isMasked = (secondByte & 0x80) != 0;
        var payloadLengthIndicator = secondByte & 0x7F;
        var header = ReadHeader(span, payloadLengthIndicator, isMasked);

        if (header is null)
        {
            return null;
        }

        if (WebSocketOpcodes.HasControlBehavior(opcode) && header.PayloadLength > 125)
        {
            throw new System.IO.InvalidDataException("Control frames must not exceed 125 bytes of payload.");
        }

        if (header.PayloadLength > int.MaxValue)
        {
            throw new System.IO.InvalidDataException("WebSocket payload exceeds supported size.");
        }

        if (header.PayloadLength > MaxFramePayloadBytes)
        {
            throw new System.IO.InvalidDataException("WebSocket payload exceeds maximum.");
        }

        var payloadLength = (int)header.PayloadLength;
        var totalLength = checked(header.HeaderLength + payloadLength);

        if (span.Length < totalLength)
        {
            return null;
        }

        var payload = ExtractPayload(span.Slice(header.HeaderLength, payloadLength), header.MaskingKey);
        var frame = new WebSocketFrame(isFinalFragment, opcode, payload, totalLength);
        return frame;
    }

    private static ReadOnlyMemory<byte> ExtractPayload(ReadOnlySpan<byte> rawPayload, byte[]? maskingKey)
    {
        var payloadBytes = rawPayload.ToArray();

        if (maskingKey is not null)
        {
            for (var index = 0; index < payloadBytes.Length; index++)
            {
                payloadBytes[index] ^= maskingKey[index % 4];
            }
        }

        return payloadBytes;
    }

    private static WebSocketFrameHeader? ReadHeader(
        ReadOnlySpan<byte> span,
        int payloadLengthIndicator,
        bool isMasked)
    {
        var headerLength = 2;
        long payloadLength;

        if (payloadLengthIndicator <= 125)
        {
            payloadLength = payloadLengthIndicator;
        }
        else if (payloadLengthIndicator == 126)
        {
            if (span.Length < headerLength + 2)
            {
                return null;
            }

            payloadLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(headerLength, 2));
            headerLength += 2;
        }
        else
        {
            if (span.Length < headerLength + 8)
            {
                return null;
            }

            var extendedPayloadLength = BinaryPrimitives.ReadUInt64BigEndian(span.Slice(headerLength, 8));
            if ((extendedPayloadLength & 0x8000000000000000UL) != 0)
            {
                throw new System.IO.InvalidDataException("WebSocket 64-bit payload length must not have the high bit set (RFC 6455 §5.2).");
            }

            payloadLength = (long)extendedPayloadLength;
            headerLength += 8;
        }

        byte[]? maskingKey = null;
        if (isMasked)
        {
            if (span.Length < headerLength + 4)
            {
                return null;
            }

            maskingKey = span.Slice(headerLength, 4).ToArray();
            headerLength += 4;
        }

        var result = new WebSocketFrameHeader(headerLength, payloadLength, maskingKey);
        return result;
    }
}
