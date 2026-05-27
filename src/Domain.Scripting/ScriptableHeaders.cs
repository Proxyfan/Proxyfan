using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Mutable, case-insensitive header collection used by user scripts. Mirrors
///     <see cref="Proxyfan.Domain.Traffic.HeaderCollection" /> semantics but is mutable for
///     ergonomic in-script edits.
/// </summary>
public sealed class ScriptableHeaders
{
    private readonly Dictionary<string, string> _headers;

    /// <summary>
    ///     Gets the count of distinct header names.
    /// </summary>
    public int Count => _headers.Count;

    /// <summary>
    ///     Initializes a new <see cref="ScriptableHeaders" /> with no headers.
    /// </summary>
    public ScriptableHeaders()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _headers = headers;
    }

    /// <summary>
    ///     Enumerates all stored headers.
    /// </summary>
    /// <returns>An enumeration of name-value pairs.</returns>
    public IEnumerable<KeyValuePair<string, string>> Enumerate()
    {
        return _headers;
    }

    /// <summary>
    ///     Gets the value of the named header, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The header value, or null.</returns>
    public string? Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_headers.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied header is present.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns><see langword="true" /> when present.</returns>
    public bool HasHeader(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _headers.ContainsKey(name);
    }

    /// <summary>
    ///     Removes the header with the supplied name. Returns true when removed.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns><see langword="true" /> when the header was removed.</returns>
    public bool HasRemoved(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _headers.Remove(name);
    }

    /// <summary>
    ///     Sets the value of the named header, replacing any existing value.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    public void Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _headers[name] = value;
    }
}
