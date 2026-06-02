using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Serialization.Tests;

/// <summary>
///     Tests for <see cref="ProtobufDecoder" /> covering all wire types.
/// </summary>
public sealed class ProtobufDecoderTests
{
    /// <summary>
    ///     Verifies that a single varint field (field number 1, value 150) is decoded.
    /// </summary>
    [Test]
    public async Task Decode_SingleVarint_DecodesValue()
    {
        var payload = new byte[] { 0x08, 0x96, 0x01 };

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields.Count).IsEqualTo(1);
        await Assert.That(fields[0].FieldNumber).IsEqualTo(1);
        await Assert.That(fields[0].WireType).IsEqualTo(ProtobufWireType.Varint);
        await Assert.That((ulong)fields[0].Value).IsEqualTo(150UL);
    }

    /// <summary>
    ///     Verifies that a length-delimited string (field number 2, "testing") is decoded.
    /// </summary>
    [Test]
    public async Task Decode_LengthDelimitedString_DecodesBytes()
    {
        var stringBytes = Encoding.UTF8.GetBytes("testing");
        var payload = new byte[2 + stringBytes.Length];
        payload[0] = 0x12;
        payload[1] = (byte)stringBytes.Length;
        stringBytes.CopyTo(payload, 2);

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields[0].FieldNumber).IsEqualTo(2);
        await Assert.That(fields[0].WireType).IsEqualTo(ProtobufWireType.LengthDelimited);
        var bytes = (byte[])fields[0].Value;
        await Assert.That(Encoding.UTF8.GetString(bytes)).IsEqualTo("testing");
    }

    /// <summary>
    ///     Verifies that a fixed I32 (field number 1, value 7) is decoded.
    /// </summary>
    [Test]
    public async Task Decode_Fixed32_DecodesUInt32()
    {
        var payload = new byte[] { 0x0D, 0x07, 0x00, 0x00, 0x00 };

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields[0].WireType).IsEqualTo(ProtobufWireType.I32);
        await Assert.That((uint)fields[0].Value).IsEqualTo(7U);
    }

    /// <summary>
    ///     Verifies that a fixed I64 (field number 1, value 42) is decoded.
    /// </summary>
    [Test]
    public async Task Decode_Fixed64_DecodesUInt64()
    {
        var payload = new byte[] { 0x09, 0x2A, 0, 0, 0, 0, 0, 0, 0 };

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields[0].WireType).IsEqualTo(ProtobufWireType.I64);
        await Assert.That((ulong)fields[0].Value).IsEqualTo(42UL);
    }

    /// <summary>
    ///     Verifies that two consecutive fields are decoded in order.
    /// </summary>
    [Test]
    public async Task Decode_TwoFields_DecodesBoth()
    {
        var payload = new byte[] { 0x08, 0x01, 0x10, 0x02 };

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields.Count).IsEqualTo(2);
        await Assert.That(fields[0].FieldNumber).IsEqualTo(1);
        await Assert.That(fields[1].FieldNumber).IsEqualTo(2);
    }

    /// <summary>
    ///     Verifies that an empty payload returns no fields.
    /// </summary>
    [Test]
    public async Task Decode_EmptyPayload_ReturnsEmpty()
    {
        var fields = ProtobufDecoder.Decode(System.Array.Empty<byte>());

        await Assert.That(fields.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that a truncated varint throws.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedVarint_Throws()
    {
        var payload = new byte[] { 0x08, 0x80 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a truncated length-delimited field throws.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedLengthDelimited_Throws()
    {
        var payload = new byte[] { 0x12, 0x10, 0x00 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a truncated I32 throws.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedI32_Throws()
    {
        var payload = new byte[] { 0x0D, 0x00, 0x00 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a truncated I64 throws.
    /// </summary>
    [Test]
    public async Task Decode_TruncatedI64_Throws()
    {
        var payload = new byte[] { 0x09, 0x00, 0x00 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that deprecated group wire types throw.
    /// </summary>
    [Test]
    public async Task Decode_StartGroup_Throws()
    {
        var payload = new byte[] { 0x0B };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that the EndGroup deprecated wire type (4) is also rejected.
    /// </summary>
    [Test]
    public async Task Decode_EndGroup_Throws()
    {
        var payload = new byte[] { 0x0C };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that an unknown wire type (e.g. 7) is rejected with a meaningful error.
    /// </summary>
    [Test]
    public async Task Decode_UnknownWireType_Throws()
    {
        var payload = new byte[] { 0x0F };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that very large varints (overflowing 64 bits) throw.
    /// </summary>
    [Test]
    public async Task Decode_VarintOverflow_Throws()
    {
        var payload = new byte[] { 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a high field number (encoded over multiple varint bytes) is decoded.
    /// </summary>
    [Test]
    public async Task Decode_HighFieldNumber_DecodesCorrectly()
    {
        var payload = new byte[] { 0xA8, 0x06, 0x01 };

        var fields = ProtobufDecoder.Decode(payload);

        await Assert.That(fields[0].FieldNumber).IsEqualTo(101);
        await Assert.That((ulong)fields[0].Value).IsEqualTo(1UL);
    }

    /// <summary>
    ///     Verifies that field number zero (reserved by the protobuf wire format) is rejected.
    /// </summary>
    [Test]
    public async Task Decode_ZeroFieldNumber_Throws()
    {
        var payload = new byte[] { 0x00, 0x01 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }

    /// <summary>
    ///     Verifies that a field number above the protobuf maximum (2^29 - 1) is rejected,
    ///     preventing very large tags from wrapping to negative field numbers.
    /// </summary>
    [Test]
    public async Task Decode_FieldNumberAboveMaximum_Throws()
    {
        // Tag varint encoding field number 2^29 (one above the protobuf maximum 2^29 - 1)
        // with wire type 0 (varint): (2^29 << 3) | 0 = 0x100000000.
        var payload = new byte[] { 0x80, 0x80, 0x80, 0x80, 0x10, 0x01 };

        await Assert.That(() => ProtobufDecoder.Decode(payload)).Throws<InvalidDataException>();
    }
}
