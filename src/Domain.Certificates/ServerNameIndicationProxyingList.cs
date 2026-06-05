using System;
using System.Collections.Generic;
using System.Threading;

namespace Proxyfan.Domain.Certificates;

/// <summary>
///     Represents the configured include and exclude patterns used to determine
///     whether secure traffic for a host name should be intercepted.
/// </summary>
public sealed class ServerNameIndicationProxyingList
{
    /// <summary>
    ///     Raised whenever the list contents or enabled state changes.
    /// </summary>
    public event ServerNameIndicationProxyingListChanged? Changed;

    private readonly HashSet<string> _excludedPatterns;
    private readonly HashSet<string> _includedPatterns;
    private readonly Lock _syncRoot;

    /// <summary>
    ///     Gets the excluded host name patterns currently configured.
    /// </summary>
    public IReadOnlyCollection<string> ExcludedPatterns => _excludedPatterns;

    /// <summary>
    ///     Gets the included host name patterns currently configured.
    /// </summary>
    public IReadOnlyCollection<string> IncludedPatterns => _includedPatterns;

    /// <summary>
    ///     Gets a value indicating whether server name indication proxying is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerNameIndicationProxyingList" /> class.
    /// </summary>
    /// <param name="isEnabled">A value indicating whether proxying is enabled.</param>
    public ServerNameIndicationProxyingList(bool isEnabled)
    {
        var excludedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var includedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syncRoot = new Lock();
        _excludedPatterns = excludedPatterns;
        _includedPatterns = includedPatterns;
        _syncRoot = syncRoot;
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

        bool hasChanged;
        lock (_syncRoot)
        {
            hasChanged = _excludedPatterns.Add(pattern);
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
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

        bool hasChanged;
        lock (_syncRoot)
        {
            hasChanged = _includedPatterns.Add(pattern);
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
    }

    /// <summary>
    ///     Disables server name indication proxying. Raises <see cref="Changed" /> when the state actually flips.
    /// </summary>
    public void Disable()
    {
        bool hasChanged;
        lock (_syncRoot)
        {
            if (!IsEnabled)
            {
                hasChanged = false;
            }
            else
            {
                IsEnabled = false;
                hasChanged = true;
            }
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
    }

    /// <summary>
    ///     Enables server name indication proxying. Raises <see cref="Changed" /> when the state actually flips.
    /// </summary>
    public void Enable()
    {
        bool hasChanged;
        lock (_syncRoot)
        {
            if (IsEnabled)
            {
                hasChanged = false;
            }
            else
            {
                IsEnabled = true;
                hasChanged = true;
            }
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
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
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return false;
        }

        bool isEnabled;
        string[] excludedPatterns;
        string[] includedPatterns;
        lock (_syncRoot)
        {
            isEnabled = IsEnabled;
            excludedPatterns = CopyPatterns(_excludedPatterns);
            includedPatterns = CopyPatterns(_includedPatterns);
        }

        if (!isEnabled)
        {
            return false;
        }

        foreach (var pattern in excludedPatterns)
        {
            if (HasPatternMatch(hostname, pattern))
            {
                return false;
            }
        }

        foreach (var pattern in includedPatterns)
        {
            if (HasPatternMatch(hostname, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes an excluded host name pattern.
    /// </summary>
    /// <param name="pattern">The excluded host name pattern.</param>
    public void RemoveExcludedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        bool hasChanged;
        lock (_syncRoot)
        {
            hasChanged = _excludedPatterns.Remove(pattern);
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
    }

    /// <summary>
    ///     Removes an included host name pattern.
    /// </summary>
    /// <param name="pattern">The included host name pattern.</param>
    public void RemoveIncludedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        bool hasChanged;
        lock (_syncRoot)
        {
            hasChanged = _includedPatterns.Remove(pattern);
        }

        if (hasChanged)
        {
            RaiseChanged();
        }
    }

    private string[] CopyPatterns(HashSet<string> patterns)
    {
        var copiedPatterns = new string[patterns.Count];
        var index = 0;
        foreach (var pattern in patterns)
        {
            copiedPatterns[index] = pattern;
            index++;
        }

        return copiedPatterns;
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

    private void RaiseChanged()
    {
        Changed?.Invoke(this);
    }
}
