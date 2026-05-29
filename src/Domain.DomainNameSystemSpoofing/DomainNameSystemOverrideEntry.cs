using System;
using System.Net;
using System.Threading;

namespace Proxyfan.Domain.DomainNameSystemSpoofing;

/// <summary>
///     A single DNS override entry. When a request targets a hostname that matches the
///     entry's <see cref="Hostname" /> pattern (case-insensitive), connect to
///     <see cref="OverrideAddress" /> instead of resolving the actual DNS record.
///     Patterns prefixed with <c>*.</c> match any sub-domain of the suffix (e.g.
///     <c>*.example.com</c> matches <c>api.example.com</c> and <c>x.y.example.com</c>
///     but not bare <c>example.com</c>). All other patterns must match the host name
///     exactly.
/// </summary>
public sealed class DomainNameSystemOverrideEntry
{
    private const string WildcardPrefix = "*.";
    private int _isEnabled;
    private int _matchCount;

    /// <summary>
    ///     Gets the canonical (lower-case, trimmed, trailing-dot-stripped) form of
    ///     <see cref="Hostname" /> used for hashing and equality.
    /// </summary>
    public string CanonicalPattern { get; }

    /// <summary>
    ///     Gets the raw host name (or wildcard pattern) configured by the user. Wildcard
    ///     patterns begin with <c>*.</c>; all other patterns are treated as exact matches.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     Gets or sets whether this entry participates in lookups. Disabled entries are
    ///     skipped by <see cref="DomainNameSystemOverrideMap.Resolve" /> and do not
    ///     increment the match counter.
    /// </summary>
    public bool IsEnabled
    {
        get => Volatile.Read(ref _isEnabled) != 0;
        set => Volatile.Write(ref _isEnabled, value ? 1 : 0);
    }

    /// <summary>
    ///     Gets the kind of pattern stored in <see cref="Hostname" />.
    /// </summary>
    public DomainOverrideKind Kind { get; }

    /// <summary>
    ///     Gets the number of times this entry has been used to resolve an outbound
    ///     connection. Incremented atomically on every successful match.
    /// </summary>
    public int MatchCount => Volatile.Read(ref _matchCount);

    /// <summary>
    ///     Gets the IP address to connect to instead of the hostname's real DNS record.
    /// </summary>
    public IPAddress OverrideAddress { get; }

    /// <summary>
    ///     Gets the canonical suffix used to match wildcard patterns (e.g. for the pattern
    ///     <c>*.Example.com</c> this is <c>.example.com</c>). For
    ///     <see cref="DomainOverrideKind.Exact" /> entries this is empty.
    /// </summary>
    public string WildcardSuffix { get; }

    /// <summary>
    ///     Initializes a new <see cref="DomainNameSystemOverrideEntry" /> in the enabled
    ///     state with a zero match counter.
    /// </summary>
    /// <param name="hostname">The host name or wildcard pattern to match.</param>
    /// <param name="overrideAddress">The IP address to substitute.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="hostname" /> is null, whitespace, contains only the
    ///     <c>*.</c> prefix, or contains embedded whitespace.
    /// </exception>
    public DomainNameSystemOverrideEntry(string hostname, IPAddress overrideAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        var trimmed = hostname.Trim();
        if (trimmed.Contains(' ', StringComparison.Ordinal))
        {
            throw new ArgumentException("Hostname patterns may not contain whitespace.", nameof(hostname));
        }

        if (trimmed.StartsWith(WildcardPrefix, StringComparison.Ordinal))
        {
            var afterPrefix = trimmed[2..].TrimEnd('.');
            if (afterPrefix.Length == 0)
            {
                throw new ArgumentException("Wildcard patterns must include a domain after '*.'", nameof(hostname));
            }
        }

        var canonical = DomainPatternNormalization.Normalize(trimmed);
        if (canonical.StartsWith(WildcardPrefix, StringComparison.Ordinal))
        {
            var suffixWithoutStar = canonical[1..];
            Kind = DomainOverrideKind.WildcardSuffix;
            WildcardSuffix = suffixWithoutStar;
        }
        else
        {
            Kind = DomainOverrideKind.Exact;
            WildcardSuffix = string.Empty;
        }

        Hostname = trimmed;
        OverrideAddress = overrideAddress;
        CanonicalPattern = canonical;
        _isEnabled = 1;
        _matchCount = 0;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied (already canonical, lower-case)
    ///     hostname matches this entry's pattern.
    /// </summary>
    /// <param name="canonicalHostname">The hostname to test, in canonical form.</param>
    /// <returns><see langword="true" /> when the hostname matches.</returns>
    public bool HasMatch(string canonicalHostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalHostname);
        if (Kind == DomainOverrideKind.Exact)
        {
            return string.Equals(canonicalHostname, CanonicalPattern, StringComparison.Ordinal);
        }

        if (canonicalHostname.Length <= WildcardSuffix.Length)
        {
            return false;
        }

        return canonicalHostname.EndsWith(WildcardSuffix, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Increments the entry's match counter by one. Returns the new value so callers
    ///     can publish it without re-reading the field.
    /// </summary>
    /// <returns>The new match count after the increment.</returns>
    public int RecordMatch()
    {
        return Interlocked.Increment(ref _matchCount);
    }

    /// <summary>
    ///     Resets the match counter to zero. Used by the UI's reset action.
    /// </summary>
    public void ResetMatchCount()
    {
        Volatile.Write(ref _matchCount, 0);
    }
}
