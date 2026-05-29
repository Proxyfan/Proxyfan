using System;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     Helpers for searching arrays of <see cref="DomainNameSystemOverrideEntry" /> by
///     canonical pattern.
/// </summary>
public static class DomainNameSystemOverrideEntryArrays
{
    /// <summary>
    ///     Returns the index of the entry whose
    ///     <see cref="DomainNameSystemOverrideEntry.CanonicalPattern" /> equals
    ///     <paramref name="canonical" /> via ordinal comparison, or <c>-1</c> when no
    ///     entry matches.
    /// </summary>
    /// <param name="entries">The entries to search.</param>
    /// <param name="canonical">The canonical pattern to find.</param>
    /// <returns>The index, or -1.</returns>
    public static int IndexOf(DomainNameSystemOverrideEntry[] entries, string canonical)
    {
        for (var index = 0; index < entries.Length; index += 1)
        {
            if (string.Equals(entries[index].CanonicalPattern, canonical, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
