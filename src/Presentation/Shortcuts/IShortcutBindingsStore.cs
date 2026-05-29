using System.Collections.Generic;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     Persistence abstraction for the user's keyboard shortcut bindings. The shortcut
///     customization UI loads the persisted bindings on open and saves them on apply.
/// </summary>
public interface IShortcutBindingsStore
{
    /// <summary>
    ///     Loads the persisted bindings. Returns an empty dictionary when no bindings have
    ///     been stored or the stored data is malformed; the registry merges missing entries
    ///     with the default bindings.
    /// </summary>
    /// <returns>The persisted action → gesture map.</returns>
    IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Load();

    /// <summary>
    ///     Persists the supplied bindings, replacing any previously stored bindings.
    /// </summary>
    /// <param name="bindings">The bindings to persist.</param>
    void Save(IReadOnlyDictionary<ShortcutAction, KeyboardGesture> bindings);
}
