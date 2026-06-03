using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Configuration;

/// <summary>
///     Immutable, case-insensitive snapshot of configuration key-value pairs loaded from a
///     configuration source (file, environment, defaults). Supports typed value access with
///     fallback defaults.
/// </summary>
public sealed class ConfigurationSnapshot
{
    private readonly Dictionary<string, string> _values;

    /// <summary>
    ///     Gets the number of key-value pairs in the snapshot.
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    ///     Initializes a new <see cref="ConfigurationSnapshot" /> with the supplied key-value pairs.
    /// </summary>
    /// <param name="values">The key-value pairs to store. Keys are case-insensitive.</param>
    public ConfigurationSnapshot(IReadOnlyDictionary<string, string> values)
    {
        var dictionary = new Dictionary<string, string>(values.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            dictionary[pair.Key] = pair.Value;
        }

        _values = dictionary;
    }

    /// <summary>
    ///     Enumerates all stored key-value pairs.
    /// </summary>
    /// <returns>An enumeration of name-value pairs.</returns>
    public IEnumerable<KeyValuePair<string, string>> Enumerate()
    {
        return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns the value for the supplied key, or <paramref name="defaultValue" /> when absent.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns>The stored value or the default.</returns>
    public string Get(string key, string defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_values.TryGetValue(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Returns the integer value for the supplied key, or <paramref name="defaultValue" />
    ///     when the key is absent or cannot be parsed.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <returns>The parsed integer or the default.</returns>
    public int GetInteger(string key, int defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_values.TryGetValue(key, out var value)
            && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Tries to read the boolean value for the supplied key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The parsed value when the key is present and parseable.</param>
    /// <returns><see langword="true" /> when a parseable boolean was found.</returns>
    public bool HasBoolean(string key, out bool value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_values.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the snapshot contains the supplied key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns><see langword="true" /> when present.</returns>
    public bool HasKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _values.ContainsKey(key);
    }
}
