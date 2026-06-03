using System;
using System.Text;

namespace Proxyfan.Framework.Networking;

/// <summary>
///     A single HPACK header field consisting of a name, value, and a flag
///     indicating whether the field is sensitive and must never be added to
///     the dynamic table (RFC 7541 § 6.2.3 "Literal Header Field Never Indexed").
/// </summary>
public sealed class HypertextTransferProtocolVersion2HpackHeaderField
{
    /// <summary>
    ///     RFC 7541 § 4.1 — <see cref="EntrySize" /> is the sum of the name length in octets,
    ///     value length in octets, and a 32-byte overhead representing the entry's bookkeeping.
    ///     Lengths are counted as UTF-8 octets (not UTF-16 characters) so that non-ASCII names
    ///     or values are not undercounted against the dynamic-table budget. <see cref="EntrySize" />
    ///     is cached at construction because both name and value are immutable and the property
    ///     is read repeatedly during dynamic-table operations.
    /// </summary>
    public int EntrySize { get; }

    /// <summary>
    ///     Gets a value indicating whether this header is sensitive and must be encoded as
    ///     never-indexed (RFC 7541 § 6.2.3) when sent on the wire.
    /// </summary>
    public bool IsSensitive { get; }

    /// <summary>
    ///     Gets the header name. Per RFC 7540 § 8.1.2, header names are lowercase on the wire.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the header value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Initializes a new header field. Sensitive defaults to <c>false</c>.
    /// </summary>
    /// <param name="name">The lowercase header name.</param>
    /// <param name="value">The header value (string-form; binary values must be escaped by the caller).</param>
    public HypertextTransferProtocolVersion2HpackHeaderField(string name, string value)
        : this(name, value, isSensitive: false)
    {
    }

    /// <summary>
    ///     Initializes a new header field with an explicit sensitivity flag.
    /// </summary>
    /// <param name="name">The lowercase header name.</param>
    /// <param name="value">The header value.</param>
    /// <param name="isSensitive">When <c>true</c>, the field must be encoded as never-indexed.</param>
    public HypertextTransferProtocolVersion2HpackHeaderField(string name, string value, bool isSensitive)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Value = value;
        IsSensitive = isSensitive;
        EntrySize = Encoding.UTF8.GetByteCount(name) + Encoding.UTF8.GetByteCount(value) + 32;
    }
}
