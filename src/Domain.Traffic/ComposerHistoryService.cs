using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     LRU history of Request Composer entries with star/unstar support and persistence.
///     Mirrors the behaviour described in BACKLOG.md E14 F01 UC01 T02: at most 100 entries,
///     newest first, starred entries never evicted, search by URL substring.
/// </summary>
public sealed class ComposerHistoryService
{
    /// <summary>
    ///     The default maximum number of entries retained in history before LRU eviction.
    /// </summary>
    public const int DefaultMaximumEntries = 100;
    private readonly List<ComposerHistoryEntry> _entries;
    private readonly int _maximumEntries;
    private readonly IComposerHistoryStore _store;

    /// <summary>
    ///     Gets the entries in newest-first order.
    /// </summary>
    public IReadOnlyList<ComposerHistoryEntry> Entries => _entries;

    /// <summary>
    ///     Initializes a new <see cref="ComposerHistoryService" /> with the supplied store and a
    ///     maximum history size of <see cref="DefaultMaximumEntries" />. Entries are loaded from
    ///     the store immediately.
    /// </summary>
    /// <param name="store">The persistence store used to load and save entries.</param>
    public ComposerHistoryService(IComposerHistoryStore store)
        : this(store, DefaultMaximumEntries)
    {
    }

    /// <summary>
    ///     Initializes a new <see cref="ComposerHistoryService" /> with the supplied store and
    ///     maximum history size. Entries are loaded from the store immediately.
    /// </summary>
    /// <param name="store">The persistence store used to load and save entries.</param>
    /// <param name="maximumEntries">
    ///     The maximum number of non-starred entries to retain before LRU eviction.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="maximumEntries" /> is less than 1.
    /// </exception>
    public ComposerHistoryService(IComposerHistoryStore store, int maximumEntries)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEntries, 1);
        _store = store;
        _maximumEntries = maximumEntries;
        _entries = [.. store.Load()];
    }

    /// <summary>
    ///     Adds <paramref name="entry" /> to the head of the history. Evicts the oldest
    ///     non-starred entry when the maximum is exceeded. Saves the updated history.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(ComposerHistoryEntry entry)
    {
        _entries.Insert(0, entry);
        EvictIfNecessary();
        _store.Save(_entries);
    }

    /// <summary>
    ///     Removes the entry with the supplied identifier, returning true when removed.
    /// </summary>
    /// <param name="id">The identifier to remove.</param>
    /// <returns><see langword="true" /> when an entry was removed.</returns>
    public bool HasRemoved(Guid id)
    {
        var index = _entries.FindIndex(entry => entry.Id == id);

        if (index < 0)
        {
            return false;
        }

        _entries.RemoveAt(index);
        _store.Save(_entries);
        return true;
    }

    /// <summary>
    ///     Toggles the starred flag of the entry with the supplied identifier, returning true
    ///     when toggled. Persists the updated history.
    /// </summary>
    /// <param name="id">The identifier of the entry to toggle.</param>
    /// <returns><see langword="true" /> when an entry was toggled.</returns>
    public bool HasToggledStar(Guid id)
    {
        var index = _entries.FindIndex(entry => entry.Id == id);

        if (index < 0)
        {
            return false;
        }

        var existing = _entries[index];
        var updated = new ComposerHistoryEntry
        {
            Body = existing.Body,
            Headers = existing.Headers,
            Id = existing.Id,
            IsStarred = !existing.IsStarred,
            Method = existing.Method,
            StatusCode = existing.StatusCode,
            Timestamp = existing.Timestamp,
            Url = existing.Url,
        };
        _entries[index] = updated;
        _store.Save(_entries);
        return true;
    }

    /// <summary>
    ///     Returns the entries whose URL contains <paramref name="urlSubstring" /> (case
    ///     insensitive). When the substring is null or whitespace, all entries are returned.
    /// </summary>
    /// <param name="urlSubstring">The case-insensitive URL substring to match.</param>
    /// <returns>The matching entries in newest-first order.</returns>
    public IReadOnlyList<ComposerHistoryEntry> Search(string? urlSubstring)
    {
        if (string.IsNullOrWhiteSpace(urlSubstring))
        {
            return _entries;
        }

        var filtered = new List<ComposerHistoryEntry>();

        foreach (var entry in _entries)
        {
            if (entry.Url.Contains(urlSubstring, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(entry);
            }
        }

        return filtered;
    }

    private void EvictIfNecessary()
    {
        while (_entries.Count > _maximumEntries)
        {
            var evictionIndex = -1;

            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                if (!_entries[index].IsStarred)
                {
                    evictionIndex = index;
                    break;
                }
            }

            if (evictionIndex < 0)
            {
                return;
            }

            _entries.RemoveAt(evictionIndex);
        }
    }
}
