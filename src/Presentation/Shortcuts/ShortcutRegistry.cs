using System.Collections.Generic;

namespace Proxyfan.Presentation.Shortcuts;

/// <summary>
///     In-memory registry mapping <see cref="ShortcutAction" /> values to
///     <see cref="KeyboardGesture" /> bindings. Provides conflict detection so two actions
///     cannot share the same gesture, and default bindings that match the design spec.
/// </summary>
public sealed class ShortcutRegistry
{
    private readonly Dictionary<ShortcutAction, KeyboardGesture> _bindings;

    /// <summary>
    ///     Gets a snapshot of all current bindings.
    /// </summary>
    public IReadOnlyDictionary<ShortcutAction, KeyboardGesture> Bindings => _bindings;

    /// <summary>
    ///     Initializes a new <see cref="ShortcutRegistry" /> with the default bindings.
    /// </summary>
    public ShortcutRegistry()
    {
        _bindings = DefaultShortcutBindings.Build();
    }

    /// <summary>
    ///     Returns the action bound to the supplied gesture, or null when no binding exists.
    /// </summary>
    /// <param name="gesture">The gesture to look up.</param>
    /// <returns>The bound action, or null.</returns>
    public ShortcutAction? GetAction(KeyboardGesture gesture)
    {
        foreach (var entry in _bindings)
        {
            if (entry.Value.Modifiers == gesture.Modifiers && entry.Value.Key == gesture.Key)
            {
                return entry.Key;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the gesture currently bound to the supplied action, or null when unbound.
    /// </summary>
    /// <param name="action">The action to look up.</param>
    /// <returns>The bound gesture, or null.</returns>
    public KeyboardGesture? GetGesture(ShortcutAction action)
    {
        if (_bindings.TryGetValue(action, out var gesture))
        {
            return gesture;
        }

        return null;
    }

    /// <summary>
    ///     Sets the gesture bound to <paramref name="action" />. Throws when the gesture is
    ///     already bound to another action.
    /// </summary>
    /// <param name="action">The action to bind.</param>
    /// <param name="gesture">The gesture to bind to it.</param>
    /// <exception cref="System.InvalidOperationException">
    ///     Thrown when <paramref name="gesture" /> is already bound to another action.
    /// </exception>
    public void SetBinding(ShortcutAction action, KeyboardGesture gesture)
    {
        var existingAction = GetAction(gesture);

        if (existingAction is not null && existingAction != action)
        {
            throw new System.InvalidOperationException($"Gesture '{gesture}' is already bound to '{existingAction}'.");
        }

        _bindings[action] = gesture;
    }
}
