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

    /// <summary>
    ///     Verifies the seeded-bindings constructor overlays the supplied entries on top of
    ///     the defaults.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededBindings_OverlaysSuppliedEntries()
    {
        var custom = new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Alt };
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = custom,
        };

        var registry = new ShortcutRegistry(seed);

        await Assert.That(registry.GetGesture(ShortcutAction.Find)).IsEqualTo(custom);
    }

    /// <summary>
    ///     Verifies the seeded-bindings constructor preserves defaults for entries that the
    ///     persisted file did not contain.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededBindings_PreservesUnseededDefaults()
    {
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = new() { Key = "Q", Modifiers = KeyboardModifier.Alt },
        };

        var registry = new ShortcutRegistry(seed);

        var clearTraffic = registry.GetGesture(ShortcutAction.ClearTraffic);
        await Assert.That(clearTraffic).IsNotNull();
        await Assert.That(clearTraffic!.Key).IsEqualTo("K");
        await Assert.That(clearTraffic.Modifiers).IsEqualTo(KeyboardModifier.Control);
    }

    /// <summary>
    ///     Verifies that a persisted seed entry whose gesture is still bound to another action
    ///     by default is skipped, falling back to the default for the affected action so the
    ///     registry never holds two actions on the same gesture.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededBindings_ConflictingEntryFallsBackToDefault()
    {
        // Find is seeded with Ctrl+K, which is the default gesture for ClearTraffic.
        // The conflicting seed entry must be skipped and Find must keep its default Ctrl+F,
        // leaving ClearTraffic on Ctrl+K so no two actions share a gesture.
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = new() { Key = "K", Modifiers = KeyboardModifier.Control },
        };

        var registry = new ShortcutRegistry(seed);

        var find = registry.GetGesture(ShortcutAction.Find);
        await Assert.That(find!.Key).IsEqualTo("F");
        await Assert.That(find.Modifiers).IsEqualTo(KeyboardModifier.Control);

        var clearTraffic = registry.GetGesture(ShortcutAction.ClearTraffic);
        await Assert.That(clearTraffic!.Key).IsEqualTo("K");
        await Assert.That(clearTraffic.Modifiers).IsEqualTo(KeyboardModifier.Control);

        var lookup = registry.GetAction(new KeyboardGesture { Key = "K", Modifiers = KeyboardModifier.Control });
        await Assert.That(lookup).IsEqualTo(ShortcutAction.ClearTraffic);
    }

    /// <summary>
    ///     Verifies that two seed entries swapping defaults (e.g. Find takes ClearTraffic's
    ///     Ctrl+K and ClearTraffic takes a fresh gesture) apply cleanly because clearing the
    ///     overridden defaults up-front prevents spurious conflicts.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededBindings_SwappedDefaultsApplyCleanly()
    {
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = new() { Key = "K", Modifiers = KeyboardModifier.Control },
            [ShortcutAction.ClearTraffic] = new() { Key = "L", Modifiers = KeyboardModifier.Control },
        };

        var registry = new ShortcutRegistry(seed);

        var find = registry.GetGesture(ShortcutAction.Find);
        await Assert.That(find!.Key).IsEqualTo("K");
        var clearTraffic = registry.GetGesture(ShortcutAction.ClearTraffic);
        await Assert.That(clearTraffic!.Key).IsEqualTo("L");
    }

    /// <summary>
    ///     Verifies that when two seed entries target the same gesture, only the first wins
    ///     and the second falls back to its default rather than producing duplicate bindings.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededBindings_InternalConflictFallsBackToDefault()
    {
        var sharedGesture = new KeyboardGesture { Key = "J", Modifiers = KeyboardModifier.Control };
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = sharedGesture,
            [ShortcutAction.ExportSession] = sharedGesture,
        };

        var registry = new ShortcutRegistry(seed);

        var owner = registry.GetAction(sharedGesture);
        await Assert.That(owner).IsNotNull();

        // Whichever action was applied second must not also be bound to the shared gesture.
        var find = registry.GetGesture(ShortcutAction.Find);
        var export = registry.GetGesture(ShortcutAction.ExportSession);
        await Assert.That(find).IsNotNull();
        await Assert.That(export).IsNotNull();
        await Assert.That(find!.Key == export!.Key && find.Modifiers == export.Modifiers).IsFalse();
    }

    /// <summary>
    ///     Verifies a lower-case letter key is normalized to upper-case so it matches the
    ///     canonical default binding via <see cref="KeyboardGesture.Key" />'s init-time
    ///     normalization.
    /// </summary>
    [Test]
    public async Task GetAction_LowerCaseLetterGesture_MatchesUpperCaseDefault()
    {
        var registry = new ShortcutRegistry();
        var gesture = new KeyboardGesture
        {
            Key = "r",
            Modifiers = KeyboardModifier.Control,
        };

        await Assert.That(gesture.Key).IsEqualTo("R");
        var action = registry.GetAction(gesture);
        await Assert.That(action).IsEqualTo(ShortcutAction.ToggleCapture);
    }

    /// <summary>
    ///     Verifies that a seed entry with a lower-case letter key conflicts with a default
    ///     binding on the upper-case form, exercising the case-insensitive comparison in
    ///     <see cref="ShortcutRegistry.GetAction" />.
    /// </summary>
    [Test]
    public async Task Constructor_WithSeededLowerCaseConflict_FallsBackToDefault()
    {
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>
        {
            [ShortcutAction.Find] = new() { Key = "k", Modifiers = KeyboardModifier.Control },
        };

        var registry = new ShortcutRegistry(seed);

        var find = registry.GetGesture(ShortcutAction.Find);
        await Assert.That(find!.Key).IsEqualTo("F");
        var clearTraffic = registry.GetGesture(ShortcutAction.ClearTraffic);
        await Assert.That(clearTraffic!.Key).IsEqualTo("K");
    }

    /// <summary>
    ///     Verifies multi-character key names such as <c>"Delete"</c> are preserved verbatim
    ///     and not affected by the single-letter upper-casing normalization.
    /// </summary>
    [Test]
    public async Task KeyboardGesture_MultiCharacterKey_PreservedVerbatim()
    {
        var gesture = new KeyboardGesture
        {
            Key = "Delete",
            Modifiers = KeyboardModifier.None,
        };

        await Assert.That(gesture.Key).IsEqualTo("Delete");
    }

    /// <summary>
    ///     Verifies the seeded-bindings constructor with an empty dictionary yields the full
    ///     default binding set.
    /// </summary>
    [Test]
    public async Task Constructor_WithEmptySeed_YieldsAllDefaults()
    {
        var seed = new System.Collections.Generic.Dictionary<ShortcutAction, KeyboardGesture>();

        var registry = new ShortcutRegistry(seed);

        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            await Assert.That(registry.GetGesture(action)).IsNotNull();
        }
    }
}
