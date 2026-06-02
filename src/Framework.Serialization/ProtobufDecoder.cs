using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     Schema-less protobuf decoder that parses the binary wire format without requiring
///     .proto definitions. Yields a flat list of <see cref="ProtobufField" /> records keyed
///     by field number.
/// </summary>
public static class ProtobufDecoder
{
    /// <summary>
    ///     The maximum field number permitted by the protobuf wire format (2^29 - 1).
    /// </summary>
    private const ulong MaximumFieldNumber = 536870911UL;

    /// <summary>
    ///     Decodes the supplied protobuf payload into a list of fields.
    /// </summary>
    /// <param name="payload">The encoded protobuf bytes.</param>
    /// <returns>The decoded fields in occurrence order.</returns>
    /// <exception cref="System.IO.InvalidDataException">
    ///     Thrown when the payload is truncated or contains an unsupported wire type.
    /// </exception>
    public static IReadOnlyList<ProtobufField> Decode(ReadOnlyMemory<byte> payload)
    {
        var fields = new List<ProtobufField>();
        var span = payload.Span;
        var cursor = new ProtobufCursor(0);

        while (cursor.Offset < span.Length)
        {
            var tag = ReadVarint(cursor, span);
            var fieldNumberRaw = tag >> 3;
            if (fieldNumberRaw is 0 or > MaximumFieldNumber)
            {
                throw new System.IO.InvalidDataException($"Invalid protobuf field number: {fieldNumberRaw}.");
            }

            var fieldNumber = (int)fieldNumberRaw;
            var wireType = (ProtobufWireType)(int)(tag & 0x7);
            var field = ReadField(cursor, span, fieldNumber, wireType);
            fields.Add(field);
        }

        return fields;
    }

    private static ProtobufField ReadField(ProtobufCursor cursor, ReadOnlySpan<byte> span, int fieldNumber, ProtobufWireType wireType)
    {
        return wireType switch
        {
            ProtobufWireType.Varint => ReadVarintField(cursor, span, fieldNumber),
            ProtobufWireType.I64 => ReadFixed64Field(cursor, span, fieldNumber),
            ProtobufWireType.I32 => ReadFixed32Field(cursor, span, fieldNumber),
            ProtobufWireType.LengthDelimited => ReadLengthDelimitedField(cursor, span, fieldNumber),
            ProtobufWireType.StartGroup or ProtobufWireType.EndGroup => throw new System.IO.InvalidDataException("Group wire types are deprecated and not supported."),
            _ => throw new System.IO.InvalidDataException($"Unknown wire type: {(int)wireType}."),
        };
    }

    private static ProtobufField ReadFixed32Field(ProtobufCursor cursor, ReadOnlySpan<byte> span, int fieldNumber)
    {
        if (cursor.Offset + 4 > span.Length)
        {
            throw new System.IO.InvalidDataException("Truncated I32 field.");
        }

        var value = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(cursor.Offset, 4));
        cursor.Advance(4);
        var field = new ProtobufField(fieldNumber, ProtobufWireType.I32, value);
        return field;
    }

    private static ProtobufField ReadFixed64Field(ProtobufCursor cursor, ReadOnlySpan<byte> span, int fieldNumber)
    {
        if (cursor.Offset + 8 > span.Length)
        {
            throw new System.IO.InvalidDataException("Truncated I64 field.");
        }

        var value = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(cursor.Offset, 8));
        cursor.Advance(8);
        var field = new ProtobufField(fieldNumber, ProtobufWireType.I64, value);
        return field;
    }

    private static ProtobufField ReadLengthDelimitedField(ProtobufCursor cursor, ReadOnlySpan<byte> span, int fieldNumber)
    {
        var length = (int)ReadVarint(cursor, span);
        if (cursor.Offset + length > span.Length)
        {
            throw new System.IO.InvalidDataException("Truncated length-delimited field.");
        }

        var bytes = span.Slice(cursor.Offset, length).ToArray();
        cursor.Advance(length);
        var field = new ProtobufField(fieldNumber, ProtobufWireType.LengthDelimited, bytes);
        return field;
    }

    private static ulong ReadVarint(ProtobufCursor cursor, ReadOnlySpan<byte> span)
    {
        ulong result = 0;
        var shift = 0;

        while (true)
        {
            if (cursor.Offset >= span.Length)
            {
                throw new System.IO.InvalidDataException("Truncated varint.");
            }

            var current = span[cursor.Offset];
            cursor.Advance(1);
            result |= (ulong)(current & 0x7F) << shift;

            if ((current & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
            if (shift >= 64)
            {
                throw new System.IO.InvalidDataException("Varint overflows 64 bits.");
            }
        }
    }

    private static ProtobufField ReadVarintField(ProtobufCursor cursor, ReadOnlySpan<byte> span, int fieldNumber)
    {
        var value = ReadVarint(cursor, span);
        var field = new ProtobufField(fieldNumber, ProtobufWireType.Varint, value);
        return field;
    }
}
