using System.Collections.Generic;
using System.IO;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     File-backed <see cref="IShortcutBindingsStore" />. Reads from and writes to a JSON
///     file using <see cref="ShortcutBindingsJsonSerializer" />. This is the production
///     store used by the customization UI to persist shortcut bindings to
///     <c>%LOCALAPPDATA%\Proxyfan\shortcuts.json</c>.
/// </summary>
public sealed class FileShortcutBindingsStore : IShortcutBindingsStore
{
    private readonly string _filePath;

    /// <summary>
    ///     Initializes a new <see cref="FileShortcutBindingsStore" /> backed by the supplied
    ///     file path.
    /// </summary>
    /// <param name="filePath">The absolute path to the shortcuts JSON file.</param>
    public FileShortcutBindingsStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<ShortcutAction, KeyboardGesture>();
        }

        var json = File.ReadAllText(_filePath);
        return ShortcutBindingsJsonSerializer.Deserialize(json);
    }

    /// <inheritdoc />
    public void Save(IReadOnlyDictionary<ShortcutAction, KeyboardGesture> bindings)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = ShortcutBindingsJsonSerializer.Serialize(bindings);
        File.WriteAllText(_filePath, json);
    }
}
