using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Shortcuts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Proxyfan.Client.Tools.ViewModels;

/// <summary>
///     View model for the keyboard shortcut customization tool window. Displays a grid of
///     <see cref="ShortcutAction" /> rows showing the current gesture for each action and
///     lets the user rebind any row to a new gesture (with conflict detection) or reset
///     the entire set to defaults. Changes are persisted through
///     <see cref="IShortcutBindingsStore" /> and applied to the live
///     <see cref="ShortcutRegistry" />.
/// </summary>
public sealed partial class KeyboardShortcutsViewModel : ObservableObject
{
    private const string StatusReset = "Reverted to defaults";
    private const string StatusSaved = "Saved";
    private readonly LocalizationService _localization;
    private readonly ShortcutRegistry _registry;
    private readonly IShortcutBindingsStore _store;
    [ObservableProperty]
    private KeyboardShortcutEntryViewModel? _selectedEntry;
    [ObservableProperty]
    private string _statusMessage;

    /// <summary>
    ///     Gets the observable collection of shortcut rows displayed in the grid.
    /// </summary>
    public ObservableCollection<KeyboardShortcutEntryViewModel> Bindings { get; }

    /// <summary>
    ///     Initializes a new <see cref="KeyboardShortcutsViewModel" /> from the live registry
    ///     and persistence store.
    /// </summary>
    /// <param name="registry">The live shortcut registry mutated when the user rebinds.</param>
    /// <param name="store">The store used to persist customizations.</param>
    /// <param name="localization">The localization service used to resolve action labels.</param>
    public KeyboardShortcutsViewModel(ShortcutRegistry registry, IShortcutBindingsStore store, LocalizationService localization)
    {
        _registry = registry;
        _store = store;
        _localization = localization;
        Bindings = [];
        _statusMessage = string.Empty;
        Refresh();
        _localization.PropertyChanged += OnLocalizationChanged;
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the supplied gesture can be applied to the
    ///     supplied action without conflicting with another action's binding.
    /// </summary>
    /// <param name="action">The action that would receive the gesture.</param>
    /// <param name="gesture">The proposed gesture.</param>
    /// <returns><see langword="true" /> when the rebind would succeed.</returns>
    public bool CanRebind(ShortcutAction action, KeyboardGesture gesture)
    {
        var existing = _registry.GetAction(gesture);
        return existing is null || existing == action;
    }

    /// <summary>
    ///     Rebinds the supplied action to the supplied gesture. Sets
    ///     <see cref="StatusMessage" /> to the conflict reason and leaves the binding
    ///     unchanged when the gesture is already bound to another action.
    /// </summary>
    /// <param name="action">The action to rebind.</param>
    /// <param name="gesture">The new gesture to assign.</param>
    public void Rebind(ShortcutAction action, KeyboardGesture gesture)
    {
        try
        {
            _registry.SetBinding(action, gesture);
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = exception.Message;
            return;
        }

        SyncEntryGesture(action, gesture);
        StatusMessage = string.Empty;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs args)
    {
        foreach (var entry in Bindings)
        {
            entry.ActionLabel = ShortcutActionLabels.GetLabel(entry.Action, _localization);
        }
    }

    private void Refresh()
    {
        Bindings.Clear();

        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var gesture = _registry.GetGesture(action);
            var gestureText = gesture is null ? string.Empty : gesture.ToString();
            var label = ShortcutActionLabels.GetLabel(action, _localization);
            var entry = new KeyboardShortcutEntryViewModel(action, label, gestureText);
            Bindings.Add(entry);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        var defaults = DefaultShortcutBindings.Build();

        foreach (var entry in defaults)
        {
            _registry.SetBinding(entry.Key, entry.Value);
            SyncEntryGesture(entry.Key, entry.Value);
        }

        _store.Save(SnapshotBindings());
        StatusMessage = StatusReset;
    }

    [RelayCommand]
    private void Save()
    {
        _store.Save(SnapshotBindings());
        StatusMessage = StatusSaved;
    }

    private Dictionary<ShortcutAction, KeyboardGesture> SnapshotBindings()
    {
        var snapshot = new Dictionary<ShortcutAction, KeyboardGesture>();

        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var gesture = _registry.GetGesture(action);

            if (gesture is not null)
            {
                snapshot[action] = gesture;
            }
        }

        return snapshot;
    }

    private void SyncEntryGesture(ShortcutAction action, KeyboardGesture gesture)
    {
        foreach (var entry in Bindings)
        {
            if (entry.Action == action)
            {
                entry.UpdateGesture(gesture);
                return;
            }
        }
    }
}
