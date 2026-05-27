using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HPACK header block encoder per RFC 7541 § 6. Each instance owns a dynamic table
///     and must be used for every header block sent on the same HTTP/2 connection. The
///     encoder favours indexed representations for static-table hits, literal-with-
///     incremental-indexing for new common headers, and never-indexed for sensitive
///     fields (e.g. <c>authorization</c>, <c>cookie</c>) per RFC 7541 § 7.1.3.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackEncoder
{
    private static readonly HypertextTransferProtocolVersion2HpackLiteralLayout IncrementalIndexingLayout;
    private static readonly HypertextTransferProtocolVersion2HpackLiteralLayout NeverIndexedLayout;

    /// <summary>
    ///     Gets the dynamic table backing this encoder, exposed so that callers can apply
    ///     SETTINGS_HEADER_TABLE_SIZE updates via
    ///     <see cref="HypertextTransferProtocolVersion2HpackDynamicTable.Resize" />.
    /// </summary>
    public HypertextTransferProtocolVersion2HpackDynamicTable DynamicTable { get; }

    static HypertextTransferProtocolVersion2HpackEncoder()
    {
        var incremental = new HypertextTransferProtocolVersion2HpackLiteralLayout
        {
            FlagByte = 0x40,
            PrefixBits = 6,
        };
        IncrementalIndexingLayout = incremental;
        var never = new HypertextTransferProtocolVersion2HpackLiteralLayout
        {
            FlagByte = 0x10,
            PrefixBits = 4,
        };
        NeverIndexedLayout = never;
    }

    /// <summary>
    ///     Initializes a new encoder with a default-sized dynamic table (4096 bytes).
    /// </summary>
    public HypertextTransferProtocolVersion2HpackEncoder()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        DynamicTable = table;
    }

    /// <summary>
    ///     Initializes a new encoder with a caller-supplied dynamic table.
    /// </summary>
    /// <param name="dynamicTable">The dynamic table to use for this connection.</param>
    public HypertextTransferProtocolVersion2HpackEncoder(HypertextTransferProtocolVersion2HpackDynamicTable dynamicTable)
    {
        DynamicTable = dynamicTable;
    }

    /// <summary>
    ///     Encodes <paramref name="fields" /> as a contiguous HPACK header block. Header
    ///     names are lowercased on emission to satisfy RFC 7540 § 8.1.2.
    /// </summary>
    /// <param name="fields">The headers to encode, in transmission order.</param>
    /// <returns>The serialized header block.</returns>
    public byte[] Encode(IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> fields)
    {
        using var output = new MemoryStream();
        for (var index = 0; index < fields.Count; index++)
        {
            EncodeField(output, fields[index]);
        }
        return output.ToArray();
    }

    private int CombinedDynamicIndex(int dynamicIndex)
    {
        return HypertextTransferProtocolVersion2HpackStaticTable.Count + dynamicIndex;
    }

    private void EncodeField(MemoryStream output, HypertextTransferProtocolVersion2HpackHeaderField field)
    {
        var name = field.Name.ToLowerInvariant();
        var value = field.Value;
        if (field.IsSensitive)
        {
            EncodeNeverIndexed(output, name, value);
            return;
        }
        var staticMatch = HypertextTransferProtocolVersion2HpackStaticTable.Find(name, value);
        if (staticMatch.IsExactMatch)
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteIndexed(output, staticMatch.Index);
            return;
        }
        var dynamicMatch = DynamicTable.Find(name, value);
        if (dynamicMatch.IsExactMatch)
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteIndexed(output, CombinedDynamicIndex(dynamicMatch.Index));
            return;
        }
        var nameIndex = staticMatch.Index;
        if (nameIndex == 0 && dynamicMatch.Index > 0)
        {
            nameIndex = CombinedDynamicIndex(dynamicMatch.Index);
        }
        if (nameIndex == 0)
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteLiteralWithLiteralName(output, name, value, IncrementalIndexingLayout);
        }
        else
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteLiteralWithNameIndex(output, nameIndex, value, IncrementalIndexingLayout);
        }
        var entry = new HypertextTransferProtocolVersion2HpackHeaderField(name, value);
        DynamicTable.Add(entry);
    }

    private void EncodeNeverIndexed(MemoryStream output, string name, string value)
    {
        var staticMatch = HypertextTransferProtocolVersion2HpackStaticTable.Find(name, value);
        var nameIndex = staticMatch.Index;
        if (nameIndex == 0)
        {
            var dynamicMatch = DynamicTable.Find(name, value);
            if (dynamicMatch.Index > 0)
            {
                nameIndex = CombinedDynamicIndex(dynamicMatch.Index);
            }
        }
        if (nameIndex == 0)
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteLiteralWithLiteralName(output, name, value, NeverIndexedLayout);
        }
        else
        {
            HypertextTransferProtocolVersion2HpackIndexedWriter.WriteLiteralWithNameIndex(output, nameIndex, value, NeverIndexedLayout);
        }
    }
}
