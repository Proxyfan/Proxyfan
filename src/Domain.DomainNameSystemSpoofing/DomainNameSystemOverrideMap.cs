using System;
using System.Collections.Generic;
using System.Net;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     A thread-safe-for-reads, mutable collection of DNS overrides. Lookups are
///     case-insensitive on hostname.
/// </summary>
public sealed class DomainNameSystemOverrideMap
{
    private readonly Dictionary<string, IPAddress> _overrides;

    /// <summary>
    ///     Gets the number of entries currently in the map.
    /// </summary>
    public int Count => _overrides.Count;

    /// <summary>
    ///     Initializes a new empty <see cref="DomainNameSystemOverrideMap" />.
    /// </summary>
    public DomainNameSystemOverrideMap()
    {
        var overrides = new Dictionary<string, IPAddress>(StringComparer.OrdinalIgnoreCase);
        _overrides = overrides;
    }

    /// <summary>
    ///     Adds (or replaces) the override for the supplied entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(DomainNameSystemOverrideEntry entry)
    {
        _overrides[entry.Hostname] = entry.OverrideAddress;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied hostname has a configured override.
    /// </summary>
    /// <param name="hostname">The hostname to test.</param>
    /// <returns><see langword="true" /> when an override exists.</returns>
    public bool HasOverride(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        return _overrides.ContainsKey(hostname);
    }

    /// <summary>
    ///     Removes the override for the supplied hostname. Returns true when removed.
    /// </summary>
    /// <param name="hostname">The hostname to clear.</param>
    /// <returns><see langword="true" /> when an override was removed.</returns>
    public bool HasRemoved(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        return _overrides.Remove(hostname);
    }

    /// <summary>
    ///     Returns the override address for the supplied hostname, or <see langword="null" />
    ///     when no override is configured.
    /// </summary>
    /// <param name="hostname">The hostname to look up.</param>
    /// <returns>The override address, or null.</returns>
    public IPAddress? Resolve(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        if (_overrides.TryGetValue(hostname, out var address))
        {
            return address;
        }

        return null;
    }
}
