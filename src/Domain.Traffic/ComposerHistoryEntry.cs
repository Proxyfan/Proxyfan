using System;
using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     A single entry in the Request Composer's history. Stored chronologically and persisted to
///     disk so users can replay or edit a previously composed request. Mirrors the data shown in
///     the Composer's history sidebar (method, URL, headers, body, response status, timestamp,
///     starred flag).
/// </summary>
public sealed class ComposerHistoryEntry
{
    /// <summary>
    ///     Gets the request body bytes.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    ///     Gets the request headers (name to single value).
    /// </summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>
    ///     Gets the entry identifier. Stable across reads.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the entry is starred. Starred entries are kept in
    ///     history indefinitely; unstarred entries are evicted when the LRU cap is reached.
    /// </summary>
    public required bool IsStarred { get; init; }

    /// <summary>
    ///     Gets the HTTP method.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    ///     Gets the response status code received when the entry was last sent, or null when not
    ///     yet sent.
    /// </summary>
    public required int? StatusCode { get; init; }

    /// <summary>
    ///     Gets the timestamp at which the entry was added or last modified.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the absolute or relative URL.
    /// </summary>
    public required string Url { get; init; }
}
