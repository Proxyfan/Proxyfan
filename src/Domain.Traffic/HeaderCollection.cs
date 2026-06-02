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
        _headers = headers;
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
            yield return header;
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

    /// <summary>
    ///     Mutable builder used to assemble a <see cref="HeaderCollection" /> from many header
    ///     lines in O(n) total time. Use <see cref="Build" /> once to obtain the immutable
    ///     collection; the builder should not be reused after that call.
    /// </summary>
    public sealed class Builder
    {
        private readonly Dictionary<string, List<string>> _headers;

        /// <summary>
        ///     Initializes a new empty <see cref="Builder" /> instance.
        /// </summary>
        public Builder()
        {
            var headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            _headers = headers;
        }

        /// <summary>
        ///     Appends a header value, creating the header entry if it does not yet exist.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value.
        /// </param>
        /// <returns>
        ///     The same builder instance, to allow chaining.
        /// </returns>
        public Builder Add(string name, string value)
        {
            if (!_headers.TryGetValue(name, out List<string>? values))
            {
                var newValues = new List<string>(1);
                _headers.Add(name, newValues);
                values = newValues;
            }

            values.Add(value);
            return this;
        }

        /// <summary>
        ///     Freezes the accumulated headers into a new immutable <see cref="HeaderCollection" />.
        /// </summary>
        /// <returns>
        ///     A new <see cref="HeaderCollection" /> snapshot of the accumulated headers.
        /// </returns>
        public HeaderCollection Build()
        {
            var headers = new Dictionary<string, string[]>(_headers.Count, StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, List<string>> entry in _headers)
            {
                string[] values = [.. entry.Value];
                headers.Add(entry.Key, values);
            }

            var collection = new HeaderCollection(headers);
            return collection;
        }
    }
}