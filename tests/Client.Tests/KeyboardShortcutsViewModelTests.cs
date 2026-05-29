using System.Collections.Generic;
using System.Threading.Tasks;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="KeyboardShortcutsViewModel" /> and related types.
/// </summary>
public sealed class KeyboardShortcutsViewModelTests
{
    /// <summary>
    ///     Constructor loads every <see cref="ShortcutAction" /> into the Bindings collection
    ///     with the current registry gesture text.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultRegistry_LoadsAllActionsWithGestureText()
    {
        var registry = new ShortcutRegistry();
        var store = new StubShortcutBindingsStore();
        var viewModel = new KeyboardShortcutsViewModel(registry, store);

        await Assert.That(viewModel.Bindings.Count).IsEqualTo(System.Enum.GetValues<ShortcutAction>().Length);

        foreach (var entry in viewModel.Bindings)
        {
            await Assert.That(entry.GestureText).IsNotEmpty();
        }
    }

    /// <summary>
    ///     CanRebind returns true when the gesture is unused.
    /// </summary>
    [Test]
    public async Task CanRebind_UnusedGesture_ReturnsTrue()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        var unused = new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift };

        var canRebind = viewModel.CanRebind(ShortcutAction.Find, unused);

        await Assert.That(canRebind).IsTrue();
    }

    /// <summary>
    ///     CanRebind returns true when the action is being rebound to its own current gesture.
    /// </summary>
    [Test]
    public async Task CanRebind_SameActionSameGesture_ReturnsTrue()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        var current = registry.GetGesture(ShortcutAction.Find)!;

        var canRebind = viewModel.CanRebind(ShortcutAction.Find, current);

        await Assert.That(canRebind).IsTrue();
    }

    /// <summary>
    ///     CanRebind returns false when the gesture is already bound to a different action.
    /// </summary>
    [Test]
    public async Task CanRebind_GestureBoundToOtherAction_ReturnsFalse()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        var findGesture = registry.GetGesture(ShortcutAction.Find)!;

        var canRebind = viewModel.CanRebind(ShortcutAction.ClearTraffic, findGesture);

        await Assert.That(canRebind).IsFalse();
    }

    /// <summary>
    ///     Rebind to an unused gesture updates the registry, the entry text, and clears
    ///     <see cref="KeyboardShortcutsViewModel.StatusMessage" />.
    /// </summary>
    [Test]
    public async Task Rebind_UnusedGesture_UpdatesRegistryAndEntry()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        viewModel.StatusMessage = "old message";
        var newGesture = new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift };

        viewModel.Rebind(ShortcutAction.Find, newGesture);

        await Assert.That(registry.GetGesture(ShortcutAction.Find)).IsEqualTo(newGesture);
        var entry = FindEntry(viewModel, ShortcutAction.Find);
        await Assert.That(entry.GestureText).IsEqualTo(newGesture.ToString());
        await Assert.That(viewModel.StatusMessage).IsEqualTo(string.Empty);
    }

    /// <summary>
    ///     Rebind to a conflicting gesture leaves the registry unchanged and sets
    ///     <see cref="KeyboardShortcutsViewModel.StatusMessage" /> to the conflict reason.
    /// </summary>
    [Test]
    public async Task Rebind_ConflictingGesture_SetsStatusMessageAndLeavesUnchanged()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        var findGesture = registry.GetGesture(ShortcutAction.Find)!;
        var originalClearTraffic = registry.GetGesture(ShortcutAction.ClearTraffic);

        viewModel.Rebind(ShortcutAction.ClearTraffic, findGesture);

        await Assert.That(registry.GetGesture(ShortcutAction.ClearTraffic)).IsEqualTo(originalClearTraffic);
        await Assert.That(viewModel.StatusMessage).IsNotEmpty();
        await Assert.That(viewModel.StatusMessage).Contains("Find");
    }

    /// <summary>
    ///     SaveCommand persists the current registry state through the store.
    /// </summary>
    [Test]
    public async Task SaveCommand_Executed_PersistsRegistryStateThroughStore()
    {
        var registry = new ShortcutRegistry();
        var store = new StubShortcutBindingsStore();
        var viewModel = new KeyboardShortcutsViewModel(registry, store);

        viewModel.SaveCommand.Execute(null);

        await Assert.That(store.SaveCallCount).IsEqualTo(1);
        await Assert.That(store.LastSaved!.Count).IsEqualTo(System.Enum.GetValues<ShortcutAction>().Length);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Saved");
    }

    /// <summary>
    ///     ResetCommand restores defaults, updates entries, and saves through the store.
    /// </summary>
    [Test]
    public async Task ResetCommand_AfterRebind_RestoresDefaultsAndSaves()
    {
        var registry = new ShortcutRegistry();
        var store = new StubShortcutBindingsStore();
        var viewModel = new KeyboardShortcutsViewModel(registry, store);
        viewModel.Rebind(
            ShortcutAction.Find,
            new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift });

        viewModel.ResetCommand.Execute(null);

        var defaultFind = DefaultShortcutBindings.Build()[ShortcutAction.Find];
        var actualFind = registry.GetGesture(ShortcutAction.Find);
        await Assert.That(actualFind).IsNotNull();
        await Assert.That(actualFind!.Key).IsEqualTo(defaultFind.Key);
        await Assert.That(actualFind.Modifiers).IsEqualTo(defaultFind.Modifiers);
        var entry = FindEntry(viewModel, ShortcutAction.Find);
        await Assert.That(entry.GestureText).IsEqualTo(defaultFind.ToString());
        await Assert.That(store.SaveCallCount).IsEqualTo(1);
        await Assert.That(viewModel.StatusMessage).IsEqualTo("Reverted to defaults");
    }

    /// <summary>
    ///     Setting <see cref="KeyboardShortcutsViewModel.SelectedEntry" /> raises the
    ///     PropertyChanged notification expected by Avalonia bindings.
    /// </summary>
    [Test]
    public async Task SelectedEntry_AssignNew_RaisesPropertyChanged()
    {
        var registry = new ShortcutRegistry();
        var viewModel = new KeyboardShortcutsViewModel(registry, new StubShortcutBindingsStore());
        string? raised = null;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(KeyboardShortcutsViewModel.SelectedEntry))
            {
                raised = args.PropertyName;
            }
        };

        viewModel.SelectedEntry = viewModel.Bindings[0];

        await Assert.That(raised).IsEqualTo(nameof(KeyboardShortcutsViewModel.SelectedEntry));
    }

    /// <summary>
    ///     The entry's IsRecording flag can be toggled and persists.
    /// </summary>
    [Test]
    public async Task EntryIsRecording_AssignTrue_PersistsAndRaisesPropertyChanged()
    {
        var entry = new KeyboardShortcutEntryViewModel(ShortcutAction.Find, "Find", "Ctrl+F");
        string? raised = null;
        entry.PropertyChanged += (_, args) => raised = args.PropertyName;

        entry.IsRecording = true;

        await Assert.That(entry.IsRecording).IsTrue();
        await Assert.That(raised).IsEqualTo(nameof(entry.IsRecording));
    }

    /// <summary>
    ///     UpdateGesture replaces the gesture text on the entry.
    /// </summary>
    [Test]
    public async Task EntryUpdateGesture_NewGesture_UpdatesGestureText()
    {
        var entry = new KeyboardShortcutEntryViewModel(ShortcutAction.Find, "Find", "Ctrl+F");
        var newGesture = new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Alt };

        entry.UpdateGesture(newGesture);

        await Assert.That(entry.GestureText).IsEqualTo("Alt+Q");
    }

    /// <summary>
    ///     Constructor on the entry exposes Action and ActionLabel verbatim.
    /// </summary>
    [Test]
    public async Task EntryConstructor_SuppliedValues_ExposesActionAndLabel()
    {
        var entry = new KeyboardShortcutEntryViewModel(ShortcutAction.ClearTraffic, "Clear it", "Ctrl+K");

        await Assert.That(entry.Action).IsEqualTo(ShortcutAction.ClearTraffic);
        await Assert.That(entry.ActionLabel).IsEqualTo("Clear it");
        await Assert.That(entry.GestureText).IsEqualTo("Ctrl+K");
        await Assert.That(entry.IsRecording).IsFalse();
    }

    private static KeyboardShortcutEntryViewModel FindEntry(KeyboardShortcutsViewModel viewModel, ShortcutAction action)
    {
        foreach (var entry in viewModel.Bindings)
        {
            if (entry.Action == action)
            {
                return entry;
            }
        }

        throw new System.InvalidOperationException($"Entry for {action} not found.");
    }

    private sealed class StubShortcutBindingsStore : IShortcutBindingsStore
    {
        public IReadOnlyDictionary<ShortcutAction, KeyboardGesture>? LastSaved { get; private set; }

        public int SaveCallCount { get; private set; }

        public IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Load()
        {
            return new Dictionary<ShortcutAction, KeyboardGesture>();
        }

        public void Save(IReadOnlyDictionary<ShortcutAction, KeyboardGesture> bindings)
        {
            LastSaved = bindings;
            SaveCallCount++;
        }
    }
}
