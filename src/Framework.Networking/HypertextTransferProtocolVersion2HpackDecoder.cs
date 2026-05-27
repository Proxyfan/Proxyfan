using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HPACK header block decoder per RFC 7541 § 6. Each instance owns a dynamic table
///     and is therefore stateful — call <see cref="Decode" /> on the same instance for
///     every header block belonging to the same HTTP/2 connection.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackDecoder
{
    private static readonly HypertextTransferProtocolVersion2HpackLiteralOptions IncrementalIndexingOptions;
    private static readonly HypertextTransferProtocolVersion2HpackLiteralOptions NeverIndexedOptions;
    private static readonly HypertextTransferProtocolVersion2HpackLiteralOptions WithoutIndexingOptions;

    /// <summary>
    ///     Gets the dynamic table backing this decoder, exposed so that callers can apply
    ///     SETTINGS_HEADER_TABLE_SIZE updates via
    ///     <see cref="HypertextTransferProtocolVersion2HpackDynamicTable.Resize" />.
    /// </summary>
    public HypertextTransferProtocolVersion2HpackDynamicTable DynamicTable { get; }

    static HypertextTransferProtocolVersion2HpackDecoder()
    {
        var incremental = new HypertextTransferProtocolVersion2HpackLiteralOptions
        {
            PrefixBits = 6,
            IsAppendingToDynamicTable = true,
            IsSensitive = false,
        };
        IncrementalIndexingOptions = incremental;
        var never = new HypertextTransferProtocolVersion2HpackLiteralOptions
        {
            PrefixBits = 4,
            IsAppendingToDynamicTable = false,
            IsSensitive = true,
        };
        NeverIndexedOptions = never;
        var without = new HypertextTransferProtocolVersion2HpackLiteralOptions
        {
            PrefixBits = 4,
            IsAppendingToDynamicTable = false,
            IsSensitive = false,
        };
        WithoutIndexingOptions = without;
    }

    /// <summary>
    ///     Initializes a new decoder with a default-sized dynamic table (4096 bytes).
    /// </summary>
    public HypertextTransferProtocolVersion2HpackDecoder()
    {
        var table = new HypertextTransferProtocolVersion2HpackDynamicTable();
        DynamicTable = table;
    }

    /// <summary>
    ///     Initializes a new decoder with a caller-supplied dynamic table.
    /// </summary>
    /// <param name="dynamicTable">The dynamic table to use for this connection.</param>
    public HypertextTransferProtocolVersion2HpackDecoder(HypertextTransferProtocolVersion2HpackDynamicTable dynamicTable)
    {
        DynamicTable = dynamicTable;
    }

    /// <summary>
    ///     Decodes <paramref name="source" /> into a list of header fields. Throws when the
    ///     input is malformed (truncated integers, invalid Huffman, indexed entries that
    ///     reference beyond the table, etc.).
    /// </summary>
    /// <param name="source">The encoded header block.</param>
    /// <returns>The decoded header fields, in transmission order.</returns>
    /// <exception cref="FormatException">When the header block is malformed.</exception>
    public IReadOnlyList<HypertextTransferProtocolVersion2HpackHeaderField> Decode(ReadOnlySpan<byte> source)
    {
        var result = new List<HypertextTransferProtocolVersion2HpackHeaderField>();
        var offset = 0;
        while (offset < source.Length)
        {
            var first = source[offset];
            if ((first & 0x80) == 0x80)
            {
                offset += DecodeIndexed(source[offset..], result);
            }
            else if ((first & 0xC0) == 0x40)
            {
                offset += DecodeLiteral(source[offset..], IncrementalIndexingOptions, result);
            }
            else if ((first & 0xE0) == 0x20)
            {
                offset += DecodeDynamicTableSizeUpdate(source[offset..]);
            }
            else if ((first & 0xF0) == 0x10)
            {
                offset += DecodeLiteral(source[offset..], NeverIndexedOptions, result);
            }
            else
            {
                offset += DecodeLiteral(source[offset..], WithoutIndexingOptions, result);
            }
        }
        return result;
    }

    private int DecodeDynamicTableSizeUpdate(ReadOnlySpan<byte> source)
    {
        var sizeResult = HypertextTransferProtocolVersion2HpackInteger.Decode(source, 5)
            ?? throw new FormatException("Malformed HPACK dynamic-table-size-update integer.");
        DynamicTable.Resize(sizeResult.Value);
        return sizeResult.BytesConsumed;
    }

    private int DecodeIndexed(ReadOnlySpan<byte> source, List<HypertextTransferProtocolVersion2HpackHeaderField> output)
    {
        var indexResult = HypertextTransferProtocolVersion2HpackInteger.Decode(source, 7)
            ?? throw new FormatException("Malformed HPACK indexed integer.");
        var index = indexResult.Value;
        if (index == 0)
        {
            throw new FormatException("HPACK indexed representation cannot reference index 0.");
        }
        var entry = ResolveEntry(index);
        output.Add(entry);
        return indexResult.BytesConsumed;
    }

    private int DecodeLiteral(
        ReadOnlySpan<byte> source,
        HypertextTransferProtocolVersion2HpackLiteralOptions options,
        List<HypertextTransferProtocolVersion2HpackHeaderField> output)
    {
        var nameIndexResult = HypertextTransferProtocolVersion2HpackInteger.Decode(source, options.PrefixBits)
            ?? throw new FormatException("Malformed HPACK literal index.");
        var offset = nameIndexResult.BytesConsumed;
        var nameIndex = nameIndexResult.Value;
        string name;
        if (nameIndex == 0)
        {
            var nameResult = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(source[offset..]);
            name = nameResult.Value;
            offset += nameResult.BytesConsumed;
        }
        else
        {
            var entry = ResolveEntry(nameIndex);
            name = entry.Name;
        }
        var valueResult = HypertextTransferProtocolVersion2HpackStringDecoder.Decode(source[offset..]);
        offset += valueResult.BytesConsumed;
        var field = new HypertextTransferProtocolVersion2HpackHeaderField(name, valueResult.Value, options.IsSensitive);
        output.Add(field);
        if (options.IsAppendingToDynamicTable)
        {
            DynamicTable.Add(field);
        }
        return offset;
    }

    private HypertextTransferProtocolVersion2HpackHeaderField ResolveEntry(int index)
    {
        if (index <= HypertextTransferProtocolVersion2HpackStaticTable.Count)
        {
            var staticEntry = HypertextTransferProtocolVersion2HpackStaticTable.Get(index);
            return staticEntry;
        }
        var dynamicIndex = index - HypertextTransferProtocolVersion2HpackStaticTable.Count;
        if (dynamicIndex > DynamicTable.Count)
        {
            throw new FormatException($"HPACK index {index} is out of range.");
        }
        var dynamicEntry = DynamicTable.Get(dynamicIndex);
        return dynamicEntry;
    }
}
