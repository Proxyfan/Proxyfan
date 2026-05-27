using System;

namespace Proxyfan.Domain.Traffic.Columns;

/// <summary>
///     Immutable definition of a user-configured custom column in the traffic list.
///     A custom column extracts the value of a specific HTTP header (request or response)
///     and displays it alongside the built-in columns. Identified by <see cref="Id" />
///     so the user can rename, edit, or remove an existing column without losing layout.
/// </summary>
public sealed record CustomColumnDefinition
{
    /// <summary>
    ///     Gets the human-readable column header text shown in the traffic list. Must be
    ///     non-empty. Renaming a column does not change its <see cref="Id" />.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the case-insensitive header key whose value is shown in the column (e.g.
    ///     "Content-Type", "X-Request-Id"). Must be non-empty.
    /// </summary>
    public required string HeaderKey { get; init; }

    /// <summary>
    ///     Gets the stable identifier for this column. Generated once at creation time and
    ///     used by configuration and UI layout code to track the column across renames.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the side of the exchange the column reads from (request or response).
    /// </summary>
    public required CustomColumnSource Source { get; init; }
}
