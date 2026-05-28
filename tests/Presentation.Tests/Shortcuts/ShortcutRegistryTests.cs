using System;
using System.Threading.Tasks;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Presentation.Tests.Shortcuts;

/// <summary>
///     Tests for <see cref="ShortcutRegistry" />.
/// </summary>
public sealed class ShortcutRegistryTests
{
    /// <summary>
    ///     Verifies the default bindings cover every <see cref="ShortcutAction" />.
    /// </summary>
    [Test]
    public async Task Constructor_DefaultBindings_CoversAllActions()
    {
        var registry = new ShortcutRegistry();

        foreach (var action in System.Enum.GetValues<ShortcutAction>())
        {
            await Assert.That(registry.GetGesture(action)).IsNotNull();
        }
    }

    /// <summary>
    ///     Verifies GetAction returns the matching action for a default binding.
    /// </summary>
    [Test]
    public async Task GetAction_DefaultControlR_ReturnsToggleCapture()
    {
        var registry = new ShortcutRegistry();
        var gesture = new KeyboardGesture
        {
            Key = "R",
            Modifiers = KeyboardModifier.Control,
        };

        var action = registry.GetAction(gesture);

        await Assert.That(action).IsEqualTo(ShortcutAction.ToggleCapture);
    }

    /// <summary>
    ///     Verifies GetAction returns null for an unbound gesture.
    /// </summary>
    [Test]
    public async Task GetAction_UnboundGesture_ReturnsNull()
    {
        var registry = new ShortcutRegistry();
        var gesture = new KeyboardGesture
        {
            Key = "Z",
            Modifiers = KeyboardModifier.Alt,
        };

        var action = registry.GetAction(gesture);

        await Assert.That(action).IsNull();
    }

    /// <summary>
    ///     Verifies SetBinding to a fresh gesture replaces the default.
    /// </summary>
    [Test]
    public async Task SetBinding_NewGesture_ReplacesDefault()
    {
        var registry = new ShortcutRegistry();
        var gesture = new KeyboardGesture
        {
            Key = "G",
            Modifiers = KeyboardModifier.Control | KeyboardModifier.Alt,
        };

        registry.SetBinding(ShortcutAction.Find, gesture);

        var stored = registry.GetGesture(ShortcutAction.Find);
        await Assert.That(stored!.Key).IsEqualTo("G");
    }

    /// <summary>
    ///     Verifies SetBinding throws when the gesture is already bound to another action.
    /// </summary>
    [Test]
    public async Task SetBinding_GestureBoundElsewhere_Throws()
    {
        var registry = new ShortcutRegistry();
        var conflict = new KeyboardGesture
        {
            Key = "K",
            Modifiers = KeyboardModifier.Control,
        };

        await Assert.That(() => registry.SetBinding(ShortcutAction.Find, conflict)).Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     Verifies KeyboardGesture renders modifiers in canonical order.
    /// </summary>
    [Test]
    public async Task ToString_ControlShiftKey_RendersCanonical()
    {
        var gesture = new KeyboardGesture
        {
            Key = "B",
            Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift,
        };

        await Assert.That(gesture.ToString()).IsEqualTo("Ctrl+Shift+B");
    }

    /// <summary>
    ///     Verifies KeyboardGesture renders all four modifier flags when present.
    /// </summary>
    [Test]
    public async Task ToString_AllModifiers_RendersAllFour()
    {
        var gesture = new KeyboardGesture
        {
            Key = "X",
            Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift | KeyboardModifier.Alt | KeyboardModifier.Meta,
        };

        await Assert.That(gesture.ToString()).IsEqualTo("Ctrl+Shift+Alt+Meta+X");
    }

    /// <summary>
    ///     Verifies GetGesture returns null for an action that has no binding (e.g. an
    ///     out-of-range enum value).
    /// </summary>
    [Test]
    public async Task GetGesture_UnboundAction_ReturnsNull()
    {
        var registry = new ShortcutRegistry();

        var gesture = registry.GetGesture((ShortcutAction)999);

        await Assert.That(gesture).IsNull();
    }

    /// <summary>
    ///     Re-binding an action to its existing gesture (where <c>existingAction == action</c>)
    ///     must succeed and not throw the conflict exception.
    /// </summary>
    [Test]
    public async Task SetBinding_SameGestureSameAction_DoesNotThrow()
    {
        var registry = new ShortcutRegistry();
        var existing = registry.GetGesture(ShortcutAction.ToggleCapture)!;

        registry.SetBinding(ShortcutAction.ToggleCapture, existing);

        await Assert.That(registry.GetGesture(ShortcutAction.ToggleCapture)).IsEqualTo(existing);
    }
}
