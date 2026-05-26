using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Represents the configured include and exclude patterns used to determine
///     whether secure traffic for a host name should be intercepted.
/// </summary>
public sealed class ServerNameIndicationProxyingList
{
    private readonly HashSet<string> _excludedPatterns;
    private readonly HashSet<string> _includedPatterns;

    /// <summary>
    ///     Gets a value indicating whether server name indication proxying is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerNameIndicationProxyingList" /> class.
    /// </summary>
    /// <param name="isEnabled">A value indicating whether proxying is enabled.</param>
    public ServerNameIndicationProxyingList(bool isEnabled)
    {
        var excludedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var includedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _excludedPatterns = excludedPatterns;
        _includedPatterns = includedPatterns;
        IsEnabled = isEnabled;
    }

    /// <summary>
    ///     Adds an excluded host name pattern.
    /// </summary>
    /// <param name="pattern">The excluded host name pattern.</param>
    public void AddExcludedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        _excludedPatterns.Add(pattern);
    }

    /// <summary>
    ///     Adds an included host name pattern.
    /// </summary>
    /// <param name="pattern">The included host name pattern.</param>
    public void AddIncludedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern must be provided.", nameof(pattern));
        }

        _includedPatterns.Add(pattern);
    }

    /// <summary>
    ///     Determines whether the specified host name matches the configured proxying rules.
    /// </summary>
    /// <param name="hostname">The host name to evaluate.</param>
    /// <returns>
    ///     <see langword="true" /> when proxying is enabled and the host name is included without being excluded;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public bool HasMatch(string hostname)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(hostname))
        {
            return false;
        }

        foreach (var pattern in _excludedPatterns)
        {
            if (HasPatternMatch(hostname, pattern))
            {
                return false;
            }
        }

        foreach (var pattern in _includedPatterns)
        {
            if (HasPatternMatch(hostname, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPatternMatch(string hostname, string pattern)
    {
        if (string.Equals(pattern, "*", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(hostname, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return HasWildcardPatternMatch(hostname, pattern);
    }

    private bool HasWildcardPatternMatch(string hostname, string pattern)
    {
        if (!pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = pattern[1..];
        return hostname.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(hostname, suffix[1..], StringComparison.OrdinalIgnoreCase);
    }
}