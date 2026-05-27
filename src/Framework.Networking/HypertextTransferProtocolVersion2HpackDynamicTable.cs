using System;
using System.Collections.Generic;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     HPACK dynamic header table per RFC 7541 § 2.3.2. New entries are inserted at the head
///     (index 1 within the dynamic table) and shift older entries toward the tail. Insertions
///     that exceed the current maximum table size evict the oldest entries until the entry
///     fits, or the new entry itself becomes too large and is dropped entirely.
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackDynamicTable
{
    private const int DefaultMaximumByteSize = 4096;
    private readonly LinkedList<HypertextTransferProtocolVersion2HpackHeaderField> _entries;

    /// <summary>
    ///     Gets the number of entries currently stored in the table.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    ///     Gets the cumulative size of all stored entries, in bytes (as defined by
    ///     <see cref="HypertextTransferProtocolVersion2HpackHeaderField.EntrySize" />).
    /// </summary>
    public int CurrentByteSize { get; private set; }

    /// <summary>
    ///     Gets the maximum allowed size in bytes. Insertions evict older entries until the
    ///     cumulative size fits within this limit.
    /// </summary>
    public int MaximumByteSize { get; private set; }

    /// <summary>
    ///     Initializes a new dynamic table with the default maximum size (4096 bytes) per RFC 7541 § 4.2.
    /// </summary>
    public HypertextTransferProtocolVersion2HpackDynamicTable()
        : this(DefaultMaximumByteSize)
    {
    }

    /// <summary>
    ///     Initializes a new dynamic table with an explicit maximum byte size.
    /// </summary>
    /// <param name="maximumByteSize">The maximum size in bytes the table is allowed to occupy.</param>
    public HypertextTransferProtocolVersion2HpackDynamicTable(int maximumByteSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumByteSize);
        var entries = new LinkedList<HypertextTransferProtocolVersion2HpackHeaderField>();
        _entries = entries;
        MaximumByteSize = maximumByteSize;
    }

    /// <summary>
    ///     Inserts <paramref name="field" /> at the head of the dynamic table, evicting older
    ///     entries from the tail as required. If <paramref name="field" /> by itself exceeds
    ///     <see cref="MaximumByteSize" />, the table is emptied and the field is discarded
    ///     (RFC 7541 § 4.4).
    /// </summary>
    /// <param name="field">The header field to insert.</param>
    public void Add(HypertextTransferProtocolVersion2HpackHeaderField field)
    {
        ArgumentException.ThrowIfNullOrEmpty(field?.Name);
        var newEntrySize = field.EntrySize;
        while (CurrentByteSize + newEntrySize > MaximumByteSize && _entries.Count > 0)
        {
            EvictOldest();
        }
        if (newEntrySize > MaximumByteSize)
        {
            return;
        }
        _entries.AddFirst(field);
        CurrentByteSize += newEntrySize;
    }

    /// <summary>
    ///     Removes all entries from the table.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        CurrentByteSize = 0;
    }

    /// <summary>
    ///     Locates the lowest entry whose name matches <paramref name="name" /> (case-insensitive).
    ///     If <paramref name="value" /> also matches, the result is flagged as an exact match.
    /// </summary>
    /// <param name="name">The header name to look up.</param>
    /// <param name="value">The header value to match.</param>
    /// <returns>
    ///     A lookup whose <c>Index</c> is the 1-based dynamic-table position (0 when not found)
    ///     and whose <c>IsExactMatch</c> flag is true only when both name and value matched.
    /// </returns>
    public HypertextTransferProtocolVersion2HpackTableLookup Find(string name, string value)
    {
        var nameMatch = 0;
        var position = 1;
        var current = _entries.First;
        while (current is not null)
        {
            if (string.Equals(current.Value.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(current.Value.Value, value, StringComparison.Ordinal))
                {
                    return new HypertextTransferProtocolVersion2HpackTableLookup(position, isExactMatch: true);
                }
                if (nameMatch == 0)
                {
                    nameMatch = position;
                }
            }
            current = current.Next;
            position++;
        }
        return new HypertextTransferProtocolVersion2HpackTableLookup(nameMatch, isExactMatch: false);
    }

    /// <summary>
    ///     Returns the entry at the 1-based <paramref name="index" /> within the dynamic table
    ///     (index 1 is the most recently inserted entry).
    /// </summary>
    /// <param name="index">The 1-based dynamic-table index.</param>
    /// <returns>The entry at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     When <paramref name="index" /> is outside <c>[1, Count]</c>.
    /// </exception>
    public HypertextTransferProtocolVersion2HpackHeaderField Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _entries.Count);
        var current = _entries.First;
        for (var step = 1; step < index; step++)
        {
            current = current!.Next;
        }
        return current!.Value;
    }

    /// <summary>
    ///     Updates the maximum allowed size in bytes (typically in response to a
    ///     SETTINGS_HEADER_TABLE_SIZE update). Entries are evicted as required to fit.
    /// </summary>
    /// <param name="maximumByteSize">The new maximum size in bytes.</param>
    public void Resize(int maximumByteSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumByteSize);
        MaximumByteSize = maximumByteSize;
        while (CurrentByteSize > MaximumByteSize && _entries.Count > 0)
        {
            EvictOldest();
        }
    }

    private void EvictOldest()
    {
        var last = _entries.Last!;
        CurrentByteSize -= last.Value.EntrySize;
        _entries.RemoveLast();
    }
}
