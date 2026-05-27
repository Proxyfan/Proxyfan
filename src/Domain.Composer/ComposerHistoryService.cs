using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Composer;

/// <summary>
///     Maintains an LRU-style history of composer requests. Newest entries are at index 0.
///     When capacity is exceeded, the oldest <em>unstarred</em> entry is evicted; starred
///     entries are preserved.
/// </summary>
public sealed class ComposerHistoryService
{
    /// <summary>
    ///     Default maximum number of entries retained in history.
    /// </summary>
    public const int DefaultCapacity = 100;
    private readonly int _capacity;
    private readonly List<ComposerHistoryEntry> _entries;
    private readonly Lock _gate;

    /// <summary>
    ///     Initializes a new <see cref="ComposerHistoryService" /> with the
    ///     <see cref="DefaultCapacity" />.
    /// </summary>
    public ComposerHistoryService() : this(DefaultCapacity)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ComposerHistoryService" />.
    /// </summary>
    /// <param name="capacity">Maximum entries before eviction (must be positive).</param>
    public ComposerHistoryService(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }
        _capacity = capacity;
        var entries = new List<ComposerHistoryEntry>();
        _entries = entries;
        var gate = new Lock();
        _gate = gate;
    }

    /// <summary>
    ///     Adds a new entry at the head of the history. When capacity is exceeded the
    ///     oldest unstarred entry is removed; if all are starred, the oldest starred entry
    ///     is removed.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(ComposerHistoryEntry entry)
    {
        lock (_gate)
        {
            _entries.Insert(0, entry);
            EnforceCapacityLocked();
        }
    }

    /// <summary>
    ///     Removes every entry, starred and unstarred alike.
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    ///     Toggles the starred state of the entry with the supplied id. Returns false when
    ///     the id is not present.
    /// </summary>
    /// <param name="entryId">The entry id.</param>
    /// <returns>True when toggled, false when not found.</returns>
    public bool HasToggledStar(Guid entryId)
    {
        lock (_gate)
        {
            for (var index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].Id != entryId)
                {
                    continue;
                }
                var replacement = _entries[index].WithStarred(!_entries[index].IsStarred);
                _entries[index] = replacement;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    ///     Filters the history by URL substring (case-insensitive).
    /// </summary>
    /// <param name="urlSubstring">The substring to match against each entry's URL.</param>
    /// <returns>The matching entries, newest first.</returns>
    public IReadOnlyList<ComposerHistoryEntry> Search(string urlSubstring)
    {
        lock (_gate)
        {
            var matches = new List<ComposerHistoryEntry>();
            for (var index = 0; index < _entries.Count; index++)
            {
                var entry = _entries[index];
                if (entry.Request.Url.Contains(urlSubstring, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(entry);
                }
            }
            return matches;
        }
    }

    /// <summary>
    ///     Returns a snapshot of all entries (newest first).
    /// </summary>
    /// <returns>An immutable snapshot.</returns>
    public IReadOnlyList<ComposerHistoryEntry> Snapshot()
    {
        lock (_gate)
        {
            var copy = new List<ComposerHistoryEntry>(_entries);
            return copy;
        }
    }

    private void EnforceCapacityLocked()
    {
        while (_entries.Count > _capacity)
        {
            var indexToRemove = _entries.Count - 1;
            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                if (_entries[index].IsStarred)
                {
                    continue;
                }
                indexToRemove = index;
                break;
            }
            _entries.RemoveAt(indexToRemove);
        }
    }
}
