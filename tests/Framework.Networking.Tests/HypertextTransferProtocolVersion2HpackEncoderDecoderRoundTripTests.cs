using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Round-trip tests for <see cref="HypertextTransferProtocolVersion2HpackEncoder" /> and
///     <see cref="HypertextTransferProtocolVersion2HpackDecoder" />.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackEncoderDecoderRoundTripTests
{
    /// <summary>
    ///     A header that exactly matches the static table encodes to a single indexed byte and
    ///     round-trips back to the same field.
    /// </summary>
    [Test]
    public async Task EncodeDecode_StaticTableExactMatch_RoundTripsAsIndexedByte()
    {
        var encoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var fields = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField(":method", "GET"),
            new HypertextTransferProtocolVersion2HpackHeaderField(":scheme", "https"),
        };

        var encoded = encoder.Encode(fields);
        var decoded = decoder.Decode(encoded);

        await Assert.That(encoded.Length).IsEqualTo(2);
        await Assert.That(encoded[0]).IsEqualTo((byte)0x82);
        await Assert.That(encoded[1]).IsEqualTo((byte)0x87);
        await Assert.That(decoded.Count).IsEqualTo(2);
        await Assert.That(decoded[0].Name).IsEqualTo(":method");
        await Assert.That(decoded[0].Value).IsEqualTo("GET");
        await Assert.That(decoded[1].Name).IsEqualTo(":scheme");
        await Assert.That(decoded[1].Value).IsEqualTo("https");
    }

    /// <summary>
    ///     A header whose name is in the static table but whose value is new encodes as
    ///     literal-with-incremental-indexing and round-trips back to the same field.
    /// </summary>
    [Test]
    public async Task EncodeDecode_StaticNameOnlyMatch_RoundTrips()
    {
        var encoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var fields = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField(":path", "/api/v1/items"),
        };

        var encoded = encoder.Encode(fields);
        var decoded = decoder.Decode(encoded);

        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(decoded[0].Name).IsEqualTo(":path");
        await Assert.That(decoded[0].Value).IsEqualTo("/api/v1/items");
        await Assert.That(decoder.DynamicTable.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     A multi-field block with literal-with-incremental-indexing causes both the encoder and
    ///     decoder to grow their dynamic tables in lock-step, so a second block referencing
    ///     dynamic-table entries decodes correctly.
    /// </summary>
    [Test]
    public async Task EncodeDecode_TwoBlocks_DynamicTablesStayInSync()
    {
        var encoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var firstBlock = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField("x-custom-header", "first-value"),
        };
        var secondBlock = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField("x-custom-header", "first-value"),
            new HypertextTransferProtocolVersion2HpackHeaderField("x-custom-header", "second-value"),
        };

        _ = decoder.Decode(encoder.Encode(firstBlock));
        var decodedSecond = decoder.Decode(encoder.Encode(secondBlock));

        await Assert.That(decodedSecond.Count).IsEqualTo(2);
        await Assert.That(decodedSecond[0].Name).IsEqualTo("x-custom-header");
        await Assert.That(decodedSecond[0].Value).IsEqualTo("first-value");
        await Assert.That(decodedSecond[1].Name).IsEqualTo("x-custom-header");
        await Assert.That(decodedSecond[1].Value).IsEqualTo("second-value");
    }

    /// <summary>
    ///     A sensitive header is encoded as literal-never-indexed (first nibble = 0001) and is
    ///     NOT added to the dynamic table; the decoder preserves the sensitivity flag.
    /// </summary>
    [Test]
    public async Task EncodeDecode_SensitiveField_EncodedAsNeverIndexedAndKeepsFlag()
    {
        var encoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var fields = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField("authorization", "Bearer secret", isSensitive: true),
        };

        var encoded = encoder.Encode(fields);
        var decoded = decoder.Decode(encoded);

        await Assert.That((encoded[0] & 0xF0)).IsEqualTo(0x10);
        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(decoded[0].Name).IsEqualTo("authorization");
        await Assert.That(decoded[0].Value).IsEqualTo("Bearer secret");
        await Assert.That(decoded[0].IsSensitive).IsTrue();
        await Assert.That(encoder.DynamicTable.Count).IsEqualTo(0);
        await Assert.That(decoder.DynamicTable.Count).IsEqualTo(0);
    }

    /// <summary>
    ///     A header whose name is brand new round-trips successfully and adds the name + value to
    ///     both peers' dynamic tables.
    /// </summary>
    [Test]
    public async Task EncodeDecode_LiteralWithLiteralName_RoundTrips()
    {
        var encoder = new HypertextTransferProtocolVersion2HpackEncoder();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var fields = new[]
        {
            new HypertextTransferProtocolVersion2HpackHeaderField("x-proxyfan-trace-id", "abc-123-xyz"),
        };

        var encoded = encoder.Encode(fields);
        var decoded = decoder.Decode(encoded);

        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(decoded[0].Name).IsEqualTo("x-proxyfan-trace-id");
        await Assert.That(decoded[0].Value).IsEqualTo("abc-123-xyz");
        await Assert.That(encoder.DynamicTable.Count).IsEqualTo(1);
        await Assert.That(decoder.DynamicTable.Count).IsEqualTo(1);
    }

    /// <summary>
    ///     The decoder honours an HPACK dynamic-table-size-update directive (first byte starts
    ///     with bits <c>001</c>) and resizes its table accordingly.
    /// </summary>
    [Test]
    public async Task Decode_DynamicTableSizeUpdate_ResizesTable()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        byte[] sizeUpdate = [0x20];

        _ = decoder.Decode(sizeUpdate);

        await Assert.That(decoder.DynamicTable.MaximumByteSize).IsEqualTo(0);
    }

    /// <summary>
    ///     An indexed representation that references index 0 is illegal and throws.
    /// </summary>
    [Test]
    public async Task Decode_IndexZero_Throws()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        byte[] illegal = [0x80];

        await Assert.That(() => decoder.Decode(illegal)).Throws<FormatException>();
    }

    /// <summary>
    ///     An indexed representation that references a table position past the end of the dynamic
    ///     table is malformed and throws.
    /// </summary>
    [Test]
    public async Task Decode_OutOfRangeIndex_Throws()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        byte[] tooLarge = [0xFF, 0x00];

        await Assert.That(() => decoder.Decode(tooLarge)).Throws<FormatException>();
    }
}
