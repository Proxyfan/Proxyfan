using System;
using System.Collections;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Represents an immutable, case-insensitive collection of HTTP headers.
/// </summary>
public sealed class HeaderCollection : IEnumerable<KeyValuePair<string, string[]>>
{
    private readonly Dictionary<string, string[]> _headers;

    /// <summary>
    ///     Gets a shared empty header collection instance.
    /// </summary>
    public static HeaderCollection Empty { get; }

    /// <summary>
    ///     Gets the number of distinct header names in the collection.
    /// </summary>
    public int Count => _headers.Count;

    static HeaderCollection()
    {
        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var empty = new HeaderCollection(headers);
        Empty = empty;
    }

    private HeaderCollection(Dictionary<string, string[]> headers)
    {
        _headers = CloneHeaders(headers);
    }

    /// <summary>
    ///     Returns a non-generic enumerator for the headers in this collection.
    /// </summary>
    /// <returns>
    ///     A non-generic enumerator over the stored header entries.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Returns an enumerator for the headers in this collection.
    /// </summary>
    /// <returns>
    ///     An enumerator over the stored header entries.
    /// </returns>
    public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
    {
        foreach (KeyValuePair<string, string[]> header in _headers)
        {
            string[] values = CopyValues(header.Value);
            yield return new KeyValuePair<string, string[]>(header.Key, values);
        }
    }

    /// <summary>
    ///     Returns a new collection with the specified header value appended.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     A new collection containing the added value.
    /// </returns>
    public HeaderCollection Add(string name, string value)
    {
        EnsureHeaderNameAndValueAreValid(name, value);
        Dictionary<string, string[]> headers = CloneHeaders(_headers);

        if (headers.TryGetValue(name, out string[]? existingValues))
        {
            string[] updatedValues = AppendValue(existingValues, value);
            headers[name] = updatedValues;
        }
        else
        {
            string[] values =
            [
                value,
            ];
            headers.Add(name, values);
        }

        var headerCollection = new HeaderCollection(headers);
        return headerCollection;
    }

    /// <summary>
    ///     Gets the first value associated with the specified header name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <returns>
    ///     The first header value, or <see langword="null" /> when the header is not present.
    /// </returns>
    public string? Get(string name)
    {
        if (_headers.TryGetValue(name, out string[]? values) && values.Length > 0)
        {
            return values[0];
        }

        return null;
    }

    /// <summary>
    ///     Gets all values associated with the specified header name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <returns>
    ///     The header values for the name, or an empty array when the header is not present.
    /// </returns>
    public string[] GetAll(string name)
    {
        if (_headers.TryGetValue(name, out string[]? values))
        {
            return CopyValues(values);
        }

        return [];
    }

    /// <summary>
    ///     Determines whether the specified header name exists in the collection.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when the header exists; otherwise, <see langword="false" />.
    /// </returns>
    public bool HasHeader(string name)
    {
        return _headers.ContainsKey(name);
    }

    private string[] AppendValue(string[] existingValues, string value)
    {
        var updatedValues = new string[existingValues.Length + 1];
        Array.Copy(existingValues, updatedValues, existingValues.Length);
        updatedValues[existingValues.Length] = value;
        return updatedValues;
    }

    private Dictionary<string, string[]> CloneHeaders(Dictionary<string, string[]> headers)
    {
        var clone = new Dictionary<string, string[]>(headers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string[]> header in headers)
        {
            string[] values = CopyValues(header.Value);
            clone.Add(header.Key, values);
        }

        return clone;
    }

    private string[] CopyValues(string[] values)
    {
        var copiedValues = new string[values.Length];
        Array.Copy(values, copiedValues, values.Length);
        return copiedValues;
    }

    private void EnsureHeaderNameAndValueAreValid(string name, string value)
    {
        if (name.Length == 0)
        {
            throw new ArgumentException("Header name cannot be empty.", nameof(name));
        }

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterOrDigit(character) &&
                character is not '!' and not '#' and not '$' and not '%' and not '&' and not '\'' and not '*' and not '+' and not '-' and not '.' and not '^' and not '_' and not '`' and not '|' and not '~')
            {
                throw new ArgumentException("Header name must be a valid RFC token.", nameof(name));
            }
        }

        foreach (var character in value)
        {
            if (character is '\r' or '\n' || (char.IsControl(character) && character != '\t'))
            {
                throw new ArgumentException("Header value cannot contain CR, LF, or control characters.", nameof(value));
            }
        }
    }

    /// <summary>
    ///     A mutable builder that accumulates header entries in a single underlying dictionary and
    ///     produces an immutable <see cref="HeaderCollection" /> in a single allocation step. Use
    ///     this in place of repeated <see cref="HeaderCollection.Add" /> calls when constructing a
    ///     collection of unknown size, to avoid the O(n^2) cloning cost of building incrementally
    ///     on top of <see cref="HeaderCollection.Empty" />.
    /// </summary>
    public sealed class Builder
    {
        private readonly Dictionary<string, string[]> _headers;

        /// <summary>
        ///     Initializes a new <see cref="Builder" /> with no headers.
        /// </summary>
        public Builder()
        {
            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            _headers = headers;
        }

        /// <summary>
        ///     Appends a header value to the builder. When the same name is added multiple times,
        ///     values accumulate in insertion order under that header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value to append.</param>
        public void Add(string name, string value)
        {
            HeaderCollection.Empty.EnsureHeaderNameAndValueAreValid(name, value);

            if (_headers.TryGetValue(name, out string[]? existingValues))
            {
                var updatedValues = new string[existingValues.Length + 1];
                Array.Copy(existingValues, updatedValues, existingValues.Length);
                updatedValues[existingValues.Length] = value;
                _headers[name] = updatedValues;
            }
            else
            {
                string[] values =
                [
                    value,
                ];
                _headers[name] = values;
            }
        }

        /// <summary>
        ///     Produces an immutable <see cref="HeaderCollection" /> snapshot of the headers
        ///     accumulated so far. The builder remains usable after <see cref="Build" /> and
        ///     subsequent mutations do not affect the returned collection.
        /// </summary>
        /// <returns>A new <see cref="HeaderCollection" /> containing the accumulated headers.</returns>
        public HeaderCollection Build()
        {
            var headerCollection = new HeaderCollection(_headers);
            return headerCollection;
        }
    }
}