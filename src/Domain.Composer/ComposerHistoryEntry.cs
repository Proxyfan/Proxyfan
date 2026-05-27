using System;

namespace Proxyfan.Domain.Composer;

/// <summary>
///     A single historical composer request entry. Persisted in the composer history file
///     and surfaced in the composer UI for recall, edit and resend.
/// </summary>
public sealed class ComposerHistoryEntry
{
    /// <summary>
    ///     Gets the stable identifier for this entry.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets a value indicating whether the entry is starred (favorited). Starred entries
    ///     survive eviction when the history exceeds its capacity.
    /// </summary>
    public bool IsStarred { get; }

    /// <summary>
    ///     Gets the composed request itself.
    /// </summary>
    public ComposerRequest Request { get; }

    /// <summary>
    ///     Gets the response status code (e.g. 200, 404), or null when the request has not
    ///     yet been sent or failed before receiving a response.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    ///     Gets the UTC timestamp when the entry was last sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    ///     Initializes a new <see cref="ComposerHistoryEntry" />.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="request">The composed request.</param>
    /// <param name="statusCode">The response status code (null when not sent).</param>
    /// <param name="timestamp">The timestamp when the entry was last sent.</param>
    /// <param name="isStarred">Whether the entry is starred.</param>
    public ComposerHistoryEntry(
        Guid id,
        ComposerRequest request,
        int? statusCode,
        DateTimeOffset timestamp,
        bool isStarred)
    {
        Id = id;
        Request = request;
        StatusCode = statusCode;
        Timestamp = timestamp;
        IsStarred = isStarred;
    }

    /// <summary>
    ///     Returns a copy of this entry with the starred flag toggled.
    /// </summary>
    /// <param name="isStarred">Whether the new entry should be starred.</param>
    /// <returns>A new entry with the inverse <see cref="IsStarred" /> value.</returns>
    public ComposerHistoryEntry WithStarred(bool isStarred)
    {
        var copy = new ComposerHistoryEntry(Id, Request, StatusCode, Timestamp, isStarred);
        return copy;
    }
}
