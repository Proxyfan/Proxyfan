using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Builds protobuf wire-format payloads for tests. Encodes the subset of the wire format
///     needed by the schema-aware printer and descriptor-set parser tests: varint,
///     length-delimited (string/bytes/submessage), fixed32, and fixed64 fields.
/// </summary>
public sealed class ProtobufWireWriter : IDisposable
{
    private readonly MemoryStream _buffer;

    /// <summary>
    ///     Initializes a new empty writer.
    /// </summary>
    public ProtobufWireWriter()
    {
        _buffer = new MemoryStream();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _buffer.Dispose();
    }

    /// <summary>
    ///     Returns the accumulated bytes.
    /// </summary>
    /// <returns>The encoded payload.</returns>
    public byte[] ToArray()
    {
        return _buffer.ToArray();
    }

    /// <summary>
    ///     Encodes a boolean as a varint field.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The boolean value to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteBoolField(int fieldNumber, bool value)
    {
        WriteVarintField(fieldNumber, value ? 1u : 0u);
        return this;
    }

    /// <summary>
    ///     Encodes a raw byte string as a length-delimited field.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The bytes to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteBytesField(int fieldNumber, byte[] value)
    {
        WriteTag(fieldNumber, ProtobufWireType.LengthDelimited);
        WriteVarint((ulong)value.Length);
        _buffer.Write(value, 0, value.Length);
        return this;
    }

    /// <summary>
    ///     Encodes a 32-bit fixed-length unsigned integer.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteFixed32Field(int fieldNumber, uint value)
    {
        WriteTag(fieldNumber, ProtobufWireType.I32);
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        _buffer.Write(bytes);
        return this;
    }

    /// <summary>
    ///     Encodes a 64-bit fixed-length unsigned integer.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteFixed64Field(int fieldNumber, ulong value)
    {
        WriteTag(fieldNumber, ProtobufWireType.I64);
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        _buffer.Write(bytes);
        return this;
    }

    /// <summary>
    ///     Encodes a UTF-8 string as a length-delimited field.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The string to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteStringField(int fieldNumber, string value)
    {
        var encoded = Encoding.UTF8.GetBytes(value);
        return WriteBytesField(fieldNumber, encoded);
    }

    /// <summary>
    ///     Encodes a varint field.
    /// </summary>
    /// <param name="fieldNumber">The protobuf field number.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>This writer for chaining.</returns>
    public ProtobufWireWriter WriteVarintField(int fieldNumber, ulong value)
    {
        WriteTag(fieldNumber, ProtobufWireType.Varint);
        WriteVarint(value);
        return this;
    }

    private void WriteTag(int fieldNumber, ProtobufWireType wireType)
    {
        var tag = (ulong)((fieldNumber << 3) | (int)wireType);
        WriteVarint(tag);
    }

    private void WriteVarint(ulong value)
    {
        while (value >= 0x80)
        {
            _buffer.WriteByte((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }

        _buffer.WriteByte((byte)value);
    }
}
