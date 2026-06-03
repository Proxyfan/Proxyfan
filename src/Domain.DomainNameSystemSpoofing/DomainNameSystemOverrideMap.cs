using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     A mutable, thread-safe collection of DNS overrides. Writers serialize on an
///     internal lock and publish an immutable snapshot of entries; readers walk the
///     snapshot lock-free for high-throughput resolution during outbound TCP connects.
///     Lookup precedence is fixed: enabled exact matches win first, then the longest
///     enabled wildcard suffix match.
/// </summary>
public sealed class DomainNameSystemOverrideMap
{
    private readonly Lock _writerSync;
    private volatile bool _isActive;
    private DomainNameSystemOverrideEntry[] _snapshot;

    /// <summary>
    ///     Gets the number of entries currently in the map (enabled and disabled).
    /// </summary>
    public int Count => _snapshot.Length;

    /// <summary>
    ///     Gets or sets whether DNS spoofing is currently active. When <see langword="false" />
    ///     the map returns no overrides regardless of configured entries. Defaults to
    ///     <see langword="true" /> so newly created maps behave the same as before this
    ///     master toggle was introduced.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    /// <summary>
    ///     Initializes a new empty <see cref="DomainNameSystemOverrideMap" /> with
    ///     spoofing active.
    /// </summary>
    public DomainNameSystemOverrideMap()
    {
        var lockInstance = new Lock();
        _writerSync = lockInstance;
        _snapshot = [];
        _isActive = true;
    }

    /// <summary>
    ///     Adds (or replaces by canonical pattern) the supplied entry. Replacement
    ///     preserves the new entry's <see cref="DomainNameSystemOverrideEntry.IsEnabled" />
    ///     state; any prior match count is discarded.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(DomainNameSystemOverrideEntry entry)
    {
        lock (_writerSync)
        {
            var existing = _snapshot;
            var canonical = entry.CanonicalPattern;
            var matchIndex = DomainNameSystemOverrideEntryArrays.IndexOf(existing, canonical);
            if (matchIndex >= 0)
            {
                var replaced = new DomainNameSystemOverrideEntry[existing.Length];
                Array.Copy(existing, replaced, existing.Length);
                replaced[matchIndex] = entry;
                _snapshot = replaced;
                return;
            }

            var grown = new DomainNameSystemOverrideEntry[existing.Length + 1];
            Array.Copy(existing, grown, existing.Length);
            grown[existing.Length] = entry;
            _snapshot = grown;
        }
    }

    /// <summary>
    ///     Returns the current match count for the entry whose canonical pattern matches
    ///     <paramref name="hostname" />, or <see langword="null" /> when no such entry
    ///     exists.
    /// </summary>
    /// <param name="hostname">The hostname or pattern whose counter to read.</param>
    /// <returns>The current match count, or <see langword="null" />.</returns>
    public int? GetMatchCount(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var canonical = DomainPatternNormalization.Normalize(hostname);
        var snapshot = _snapshot;
        var matchIndex = DomainNameSystemOverrideEntryArrays.IndexOf(snapshot, canonical);
        if (matchIndex < 0)
        {
            return null;
        }

        return snapshot[matchIndex].MatchCount;
    }

    /// <summary>
    ///     Returns an immutable snapshot of all entries (enabled and disabled). The
    ///     returned reference is safe to enumerate without locking.
    /// </summary>
    /// <returns>A snapshot array of every configured entry.</returns>
    public IReadOnlyList<DomainNameSystemOverrideEntry> GetSnapshot()
    {
        return _snapshot;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when an entry whose pattern matches the
    ///     supplied hostname exists in the map, regardless of whether it is enabled.
    /// </summary>
    /// <param name="hostname">The hostname or wildcard pattern to test.</param>
    /// <returns><see langword="true" /> when an override exists.</returns>
    public bool HasOverride(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var canonical = DomainPatternNormalization.Normalize(hostname);
        return DomainNameSystemOverrideEntryArrays.IndexOf(_snapshot, canonical) >= 0;
    }

    /// <summary>
    ///     Removes the entry whose canonical pattern matches the supplied hostname.
    ///     Returns <see langword="true" /> when an entry was removed.
    /// </summary>
    /// <param name="hostname">The hostname or pattern to remove.</param>
    /// <returns><see langword="true" /> when an entry was removed.</returns>
    public bool HasRemoved(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var canonical = DomainPatternNormalization.Normalize(hostname);
        lock (_writerSync)
        {
            var existing = _snapshot;
            var matchIndex = DomainNameSystemOverrideEntryArrays.IndexOf(existing, canonical);
            if (matchIndex < 0)
            {
                return false;
            }

            var shrunk = new DomainNameSystemOverrideEntry[existing.Length - 1];
            if (matchIndex > 0)
            {
                Array.Copy(existing, 0, shrunk, 0, matchIndex);
            }

            if (matchIndex < existing.Length - 1)
            {
                Array.Copy(existing, matchIndex + 1, shrunk, matchIndex, existing.Length - matchIndex - 1);
            }

            _snapshot = shrunk;
            return true;
        }
    }

    /// <summary>
    ///     Resets the match counter to zero on the entry whose canonical pattern matches
    ///     <paramref name="hostname" />. Returns <see langword="true" /> when an entry
    ///     was found (and reset); <see langword="false" /> when no entry matches.
    /// </summary>
    /// <param name="hostname">The hostname or pattern whose counter to reset.</param>
    /// <returns><see langword="true" /> when an entry was reset.</returns>
    public bool HasResetMatchCount(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var canonical = DomainPatternNormalization.Normalize(hostname);
        var snapshot = _snapshot;
        var matchIndex = DomainNameSystemOverrideEntryArrays.IndexOf(snapshot, canonical);
        if (matchIndex < 0)
        {
            return false;
        }

        snapshot[matchIndex].ResetMatchCount();
        return true;
    }

    /// <summary>
    ///     Sets the enabled state of the entry whose canonical pattern matches
    ///     <paramref name="hostname" />. Returns <see langword="true" /> when an entry
    ///     was found (and updated); <see langword="false" /> when no entry matches.
    ///     This is the single supported mutation path for the enabled flag from the UI
    ///     so that future eventing, validation, and persistence hooks have one place
    ///     to plug in.
    /// </summary>
    /// <param name="hostname">The hostname or pattern whose entry to update.</param>
    /// <param name="isEnabled">The desired enabled state.</param>
    /// <returns><see langword="true" /> when an entry was updated.</returns>
    public bool HasSetEnabled(string hostname, bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var canonical = DomainPatternNormalization.Normalize(hostname);
        var snapshot = _snapshot;
        var matchIndex = DomainNameSystemOverrideEntryArrays.IndexOf(snapshot, canonical);
        if (matchIndex < 0)
        {
            return false;
        }

        snapshot[matchIndex].IsEnabled = isEnabled;
        return true;
    }

    /// <summary>
    ///     Resolves the supplied hostname against the map. Returns the configured
    ///     override IP when an enabled entry matches (incrementing that entry's
    ///     <see cref="DomainNameSystemOverrideEntry.MatchCount" />) and the map is
    ///     active. Returns <see langword="null" /> when no enabled entry matches or
    ///     when spoofing is inactive.
    /// </summary>
    /// <param name="hostname">The hostname to look up.</param>
    /// <returns>The override address, or <see langword="null" />.</returns>
    public IPAddress? Resolve(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        if (!_isActive)
        {
            return null;
        }

        var canonical = DomainPatternNormalization.Normalize(hostname);
        var snapshot = _snapshot;
        DomainNameSystemOverrideEntry? bestWildcard = null;
        for (var index = 0; index < snapshot.Length; index += 1)
        {
            var entry = snapshot[index];
            if (!entry.IsEnabled)
            {
                continue;
            }

            if (!entry.HasMatch(canonical))
            {
                continue;
            }

            if (entry.Kind == DomainOverrideKind.Exact)
            {
                entry.RecordMatch();
                return entry.OverrideAddress;
            }

            if (bestWildcard is null || entry.WildcardSuffix.Length > bestWildcard.WildcardSuffix.Length)
            {
                bestWildcard = entry;
            }
        }

        if (bestWildcard is null)
        {
            return null;
        }

        bestWildcard.RecordMatch();
        return bestWildcard.OverrideAddress;
    }
}
