using System.Collections.Generic;

namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Abstraction over the persisted storage of Composer history entries. Implementations may
///     read/write a JSON file on disk, hold the entries in memory for tests, or roundtrip them
///     to a different backing store. The service uses this to load entries on startup and to
///     persist them after every mutation.
/// </summary>
public interface IComposerHistoryStore
{
    /// <summary>
    ///     Loads any persisted entries. Returns an empty list when no entries are stored.
    /// </summary>
    /// <returns>The loaded entries in their persisted order.</returns>
    IReadOnlyList<ComposerHistoryEntry> Load();

    /// <summary>
    ///     Persists the supplied entries, replacing any previously stored entries.
    /// </summary>
    /// <param name="entries">The entries to persist.</param>
    void Save(IReadOnlyList<ComposerHistoryEntry> entries);
}
