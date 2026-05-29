using System;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     Helpers for canonicalizing DNS hostnames and patterns into the form used by
///     <see cref="DomainNameSystemOverrideEntry" /> and
///     <see cref="DomainNameSystemOverrideMap" /> for equality and matching.
/// </summary>
public static class DomainPatternNormalization
{
    /// <summary>
    ///     Returns the canonical form of <paramref name="hostname" /> used for matching:
    ///     trimmed, lower-cased (invariant), with any trailing dot stripped.
    /// </summary>
    /// <param name="hostname">The hostname to normalize.</param>
    /// <returns>The canonical hostname.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="hostname" /> is null, empty, or whitespace.
    /// </exception>
    public static string Normalize(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var trimmed = hostname.Trim();
        if (trimmed.EndsWith('.'))
        {
            trimmed = trimmed[..^1];
        }

        return trimmed.ToLowerInvariant();
    }
}
