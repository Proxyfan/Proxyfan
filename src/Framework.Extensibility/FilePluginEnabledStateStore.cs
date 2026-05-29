using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Proxyfan.Framework.Extensibility;

/// <summary>
///     File-backed implementation of <see cref="IPluginEnabledStateStore" />. Persists
///     disabled plugin identifiers, one per line, into a plain-text file (default name
///     <c>disabled-plugins.txt</c>) inside the plugin root. Empty lines and lines beginning
///     with <c>#</c> are treated as comments. The store creates the parent directory and
///     file on first write and never throws on missing files.
/// </summary>
public sealed class FilePluginEnabledStateStore : IPluginEnabledStateStore
{
    private const string CommentPrefix = "#";
    private readonly Lock _writeLock;
    private HashSet<string> _disabledIdentifiers;

    /// <summary>
    ///     Gets the absolute path of the backing file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     Initializes a new <see cref="FilePluginEnabledStateStore" /> for the supplied file path.
    /// </summary>
    /// <param name="filePath">The absolute path of the backing file.</param>
    public FilePluginEnabledStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var writeLock = new Lock();
        FilePath = filePath;
        _writeLock = writeLock;
        _disabledIdentifiers = Load();
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetDisabledIdentifiers()
    {
        lock (_writeLock)
        {
            var snapshot = new HashSet<string>(_disabledIdentifiers, StringComparer.OrdinalIgnoreCase);
            return snapshot;
        }
    }

    /// <inheritdoc />
    public void SetEnabled(string identifier, bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        var trimmed = identifier.Trim();
        lock (_writeLock)
        {
            var updated = new HashSet<string>(_disabledIdentifiers, StringComparer.OrdinalIgnoreCase);
            if (isEnabled)
            {
                updated.Remove(trimmed);
            }
            else
            {
                updated.Add(trimmed);
            }

            Save(updated);
            _disabledIdentifiers = updated;
        }
    }

    private HashSet<string> Load()
    {
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(FilePath))
        {
            return disabled;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(FilePath);
        }
        catch (IOException)
        {
            return disabled;
        }
        catch (UnauthorizedAccessException)
        {
            return disabled;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(CommentPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            disabled.Add(trimmed);
        }

        return disabled;
    }

    private void Save(HashSet<string> disabled)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sorted = new List<string>(disabled);
        sorted.Sort(StringComparer.OrdinalIgnoreCase);
        File.WriteAllLines(FilePath, sorted);
    }
}
