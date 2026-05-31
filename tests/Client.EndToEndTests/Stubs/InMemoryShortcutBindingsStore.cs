using Proxyfan.Presentation.Shortcuts;
using System.Collections.Generic;

namespace Proxyfan.Client.EndToEndTests.Stubs;

/// <summary>
///     In-memory <see cref="IShortcutBindingsStore" /> for end-to-end tests that
///     drive <see cref="Proxyfan.Client.Tools.ViewModels.KeyboardShortcutsViewModel" />.
///     Records every <see cref="Save" /> call so tests can assert what was persisted.
/// </summary>
internal sealed class InMemoryShortcutBindingsStore : IShortcutBindingsStore
{
    private IReadOnlyDictionary<ShortcutAction, KeyboardGesture> _persisted =
        new Dictionary<ShortcutAction, KeyboardGesture>();

    /// <summary>
    ///     Gets the most recently persisted snapshot.
    /// </summary>
    public IReadOnlyDictionary<ShortcutAction, KeyboardGesture> LastSaved => _persisted;

    /// <summary>
    ///     Gets the number of times <see cref="Save" /> has been called.
    /// </summary>
    public int SaveCallCount { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Load()
    {
        return _persisted;
    }

    /// <inheritdoc />
    public void Save(IReadOnlyDictionary<ShortcutAction, KeyboardGesture> bindings)
    {
        _persisted = new Dictionary<ShortcutAction, KeyboardGesture>(bindings);
        SaveCallCount++;
    }
}
