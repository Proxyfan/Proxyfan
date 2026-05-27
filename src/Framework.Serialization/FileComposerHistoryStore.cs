using Proxyfan.Domain.Traffic;
using System.Collections.Generic;

namespace Proxyfan.Framework.Serialization;

/// <summary>
///     File-backed <see cref="IComposerHistoryStore" />. Reads from and writes to a JSON file
///     using <see cref="ComposerHistoryJsonSerializer" />. This is the production store used by
///     the Composer tool to persist user history to <c>%LOCALAPPDATA%\Proxyfan</c>.
/// </summary>
public sealed class FileComposerHistoryStore : IComposerHistoryStore
{
    private readonly string _filePath;

    /// <summary>
    ///     Initializes a new <see cref="FileComposerHistoryStore" /> backed by the supplied file
    ///     path.
    /// </summary>
    /// <param name="filePath">The absolute path to the history JSON file.</param>
    public FileComposerHistoryStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public IReadOnlyList<ComposerHistoryEntry> Load()
    {
        return ComposerHistoryJsonSerializer.ReadFromFile(_filePath);
    }

    /// <inheritdoc />
    public void Save(IReadOnlyList<ComposerHistoryEntry> entries)
    {
        ComposerHistoryJsonSerializer.WriteToFile(_filePath, entries);
    }
}
