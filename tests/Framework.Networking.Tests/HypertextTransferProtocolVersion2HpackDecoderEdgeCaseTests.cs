using System;
using System.Threading.Tasks;

namespace Proxyfan.Framework.Networking.Tests;

/// <summary>
///     Edge-case tests for <see cref="HypertextTransferProtocolVersion2HpackDecoder" />
///     covering malformed integer encodings and the never-indexed literal representation
///     which the round-trip tests do not exercise directly.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackDecoderEdgeCaseTests
{
    /// <summary>
    ///     Verifies that an indexed representation with a truncated multi-byte integer (only
    ///     the all-ones prefix byte) throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Decode_IndexedRepresentationWithTruncatedInteger_Throws()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var malformed = new byte[] { 0xFF };

        await Assert.That(() => decoder.Decode(malformed)).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies that a literal-with-incremental-indexing representation with a truncated
    ///     index integer throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Decode_LiteralIncrementalIndexingWithTruncatedIndex_Throws()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var malformed = new byte[] { 0x7F };

        await Assert.That(() => decoder.Decode(malformed)).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies that a dynamic-table-size-update representation with a truncated integer
    ///     throws <see cref="FormatException" />.
    /// </summary>
    [Test]
    public async Task Decode_DynamicTableSizeUpdateWithTruncatedInteger_Throws()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var malformed = new byte[] { 0x3F };

        await Assert.That(() => decoder.Decode(malformed)).Throws<FormatException>();
    }

    /// <summary>
    ///     Verifies that a literal-never-indexed representation with a new name (name index
    ///     zero) decodes correctly, exercising the 0x10 prefix branch in
    ///     <see cref="HypertextTransferProtocolVersion2HpackDecoder.Decode" />.
    /// </summary>
    [Test]
    public async Task Decode_LiteralNeverIndexedWithNewName_ReturnsHeaderField()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var encoded = new byte[]
        {
            0x10,
            0x03, (byte)'f', (byte)'o', (byte)'o',
            0x03, (byte)'b', (byte)'a', (byte)'r',
        };

        var decoded = decoder.Decode(encoded);

        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(decoded[0].Name).IsEqualTo("foo");
        await Assert.That(decoded[0].Value).IsEqualTo("bar");
    }

    /// <summary>
    ///     A literal-without-indexing representation (top-nibble <c>0x00</c>) decodes the
    ///     header field without adding it to the dynamic table. Exercises the
    ///     fall-through branch in <see cref="HypertextTransferProtocolVersion2HpackDecoder.Decode" />.
    /// </summary>
    [Test]
    public async Task Decode_LiteralWithoutIndexingWithNewName_ReturnsHeaderField()
    {
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder();
        var encoded = new byte[]
        {
            0x00,
            0x03, (byte)'q', (byte)'u', (byte)'x',
            0x03, (byte)'b', (byte)'a', (byte)'z',
        };

        var decoded = decoder.Decode(encoded);

        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(decoded[0].Name).IsEqualTo("qux");
        await Assert.That(decoded[0].Value).IsEqualTo("baz");
    }

    /// <summary>
    ///     A decoder constructed with an explicit dynamic table preserves table state across
    ///     decoding operations. Exercises the dynamic-table-accepting constructor.
    /// </summary>
    [Test]
    public async Task Constructor_ExplicitDynamicTable_UsesProvidedTable()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        var decoder = new HypertextTransferProtocolVersion2HpackDecoder(table);

        var encoded = new byte[]
        {
            0x40,
            0x03, (byte)'x', (byte)'-', (byte)'a',
            0x01, (byte)'1',
        };

        var decoded = decoder.Decode(encoded);

        await Assert.That(decoded.Count).IsEqualTo(1);
        await Assert.That(table.Count).IsEqualTo(1);
    }
}
