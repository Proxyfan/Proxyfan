using Proxyfan.Client.EndToEndTests.Infrastructure;
using Proxyfan.Client.EndToEndTests.Stubs;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Shortcuts;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.EndToEndTests;

/// <summary>
///     End-to-end UI tests covering the keyboard shortcut customization
///     tool window (<c>docs/DESIGN.md § 9 Keyboard Shortcuts</c>): the bindings
///     grid mirrors the active registry, rebinds succeed when the gesture is
///     free, conflicting rebinds are rejected with a status message, the
///     Save command persists through <see cref="IShortcutBindingsStore" />,
///     and Reset restores the defaults.
/// </summary>
public sealed class KeyboardShortcutsViewModelEndToEndTests : EndToEndTestBase
{
    [Test]
    public async Task Bindings_FreshViewModel_OneEntryPerShortcutAction()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));

            var expectedActionCount = Enum.GetValues<ShortcutAction>().Length;
            await Assert.That(vm.Bindings.Count).IsEqualTo(expectedActionCount);
        });
    }

    [Test]
    public async Task CanRebind_GestureCurrentlyBoundToAnotherAction_ReturnsFalse()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));
            var toggleCaptureGesture = registry.GetGesture(ShortcutAction.ToggleCapture);
            await Assert.That(toggleCaptureGesture).IsNotNull();

            var allowed = vm.CanRebind(ShortcutAction.ClearTraffic, toggleCaptureGesture!);

            await Assert.That(allowed).IsFalse();
        });
    }

    [Test]
    public async Task CanRebind_FreshGesture_ReturnsTrue()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));
            var freshGesture = new KeyboardGesture
            {
                Key = "F9",
                Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift,
            };

            var allowed = vm.CanRebind(ShortcutAction.ToggleCapture, freshGesture);

            await Assert.That(allowed).IsTrue();
        });
    }

    [Test]
    public async Task Rebind_FreshGesture_UpdatesRegistryAndEntryRow()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));
            var freshGesture = new KeyboardGesture
            {
                Key = "F9",
                Modifiers = KeyboardModifier.Control | KeyboardModifier.Shift,
            };

            vm.Rebind(ShortcutAction.ToggleCapture, freshGesture);

            await Assert.That(registry.GetGesture(ShortcutAction.ToggleCapture)).IsEqualTo(freshGesture);
            var row = vm.Bindings.First(e => e.Action == ShortcutAction.ToggleCapture);
            await Assert.That(row.GestureText).IsEqualTo("Ctrl+Shift+F9");
            await Assert.That(vm.StatusMessage).IsEqualTo(string.Empty);
        });
    }

    [Test]
    public async Task Rebind_ConflictingGesture_SetsStatusMessageAndLeavesRegistryUnchanged()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));
            var toggleCaptureGesture = registry.GetGesture(ShortcutAction.ToggleCapture);
            await Assert.That(toggleCaptureGesture).IsNotNull();

            vm.Rebind(ShortcutAction.ClearTraffic, toggleCaptureGesture!);

            // Conflict guarded → ClearTraffic must still point at its original gesture
            await Assert.That(registry.GetGesture(ShortcutAction.ToggleCapture)).IsEqualTo(toggleCaptureGesture);
            await Assert.That(vm.StatusMessage.Length).IsGreaterThan(0);
        });
    }

    [Test]
    public async Task SaveCommand_Invoked_PersistsViaStoreAndSetsStatus()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));

            vm.SaveCommand.Execute(null);

            await Assert.That(store.SaveCallCount).IsEqualTo(1);
            await Assert.That(store.LastSaved.Count).IsGreaterThan(0);
            await Assert.That(vm.StatusMessage).IsEqualTo("Saved");
        });
    }

    [Test]
    public async Task ResetCommand_Invoked_RebuildsRegistryFromDefaultsAndPersists()
    {
        await RunOnUiThreadAsync(async () =>
        {
            var registry = new ShortcutRegistry();
            var store = new InMemoryShortcutBindingsStore();
            var vm = new KeyboardShortcutsViewModel(registry, store, new LocalizationService(CultureInfo.InvariantCulture));
            // Mutate first so reset has visible effect
            vm.Rebind(ShortcutAction.ToggleCapture, new KeyboardGesture
            {
                Key = "F9",
                Modifiers = KeyboardModifier.Control,
            });

            vm.ResetCommand.Execute(null);

            var defaults = DefaultShortcutBindings.Build();
            await Assert.That(registry.GetGesture(ShortcutAction.ToggleCapture))
                        .IsEqualTo(defaults[ShortcutAction.ToggleCapture]);
            await Assert.That(store.SaveCallCount).IsEqualTo(1);
            await Assert.That(vm.StatusMessage).IsEqualTo("Reverted to defaults");
        });
    }
}
