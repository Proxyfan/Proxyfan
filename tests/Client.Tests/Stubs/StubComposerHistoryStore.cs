using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Client.Tests.Stubs;

/// <summary>
///     Stub <see cref="IComposerHistoryStore" /> that keeps entries entirely in memory. The
///     composer view-model tests use this to avoid touching the file system.
/// </summary>
public sealed class StubComposerHistoryStore : IComposerHistoryStore
{
    /// <summary>
    ///     Gets the number of times <see cref="Save" /> was invoked.
    /// </summary>
    public int SaveCallCount { get; private set; }

    /// <summary>
    ///     Gets or sets the entries currently held by the store. Mutated by <see cref="Save" />.
    /// </summary>
    public IReadOnlyList<ComposerHistoryEntry> Entries { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerHistoryEntry> Load()
    {
        return Entries;
    }

    /// <inheritdoc />
    public void Save(IReadOnlyList<ComposerHistoryEntry> entries)
    {
        Entries = [.. entries];
        SaveCallCount++;
    }
}
