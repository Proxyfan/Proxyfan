using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Presentation.Localization;
using Proxyfan.Presentation.Shortcuts;

namespace Proxyfan.Client.Tests;

/// <summary>
///     End-to-end integration tests exercising the full shortcut customization stack:
///     <see cref="FileShortcutBindingsStore" /> ↔ <see cref="ShortcutBindingsJsonSerializer" />
///     ↔ <see cref="ShortcutRegistry" /> ↔ <see cref="KeyboardShortcutsViewModel" />.
/// </summary>
public sealed class KeyboardShortcutsViewModelEndToEndTests
{
    /// <summary>
    ///     Rebinding through the view model and clicking Save writes to disk; loading a fresh
    ///     stack from that file preserves the custom binding.
    /// </summary>
    [Test]
    public async Task SaveThenReload_AfterRebind_PreservesCustomBinding()
    {
        var path = CreateTempPath();

        try
        {
            var store1 = new FileShortcutBindingsStore(path);
            var registry1 = new ShortcutRegistry(store1.Load());
            var viewModel1 = new KeyboardShortcutsViewModel(registry1, store1, new LocalizationService(CultureInfo.InvariantCulture));
            var custom = new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Alt };

            viewModel1.Rebind(ShortcutAction.Find, custom);
            viewModel1.SaveCommand.Execute(null);

            var store2 = new FileShortcutBindingsStore(path);
            var registry2 = new ShortcutRegistry(store2.Load());
            var loaded = registry2.GetGesture(ShortcutAction.Find);

            await Assert.That(loaded).IsNotNull();
            await Assert.That(loaded!.Key).IsEqualTo("Q");
            await Assert.That(loaded.Modifiers).IsEqualTo(KeyboardModifier.Alt);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    /// <summary>
    ///     Rebinding then clicking Reset wipes the persisted custom binding; a fresh stack
    ///     loads defaults for that action.
    /// </summary>
    [Test]
    public async Task ResetAfterRebind_PersistedAndReloaded_RestoresDefault()
    {
        var path = CreateTempPath();

        try
        {
            var store1 = new FileShortcutBindingsStore(path);
            var registry1 = new ShortcutRegistry(store1.Load());
            var viewModel1 = new KeyboardShortcutsViewModel(registry1, store1, new LocalizationService(CultureInfo.InvariantCulture));
            viewModel1.Rebind(ShortcutAction.Find, new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Alt });
            viewModel1.SaveCommand.Execute(null);
            viewModel1.ResetCommand.Execute(null);

            var store2 = new FileShortcutBindingsStore(path);
            var registry2 = new ShortcutRegistry(store2.Load());
            var loaded = registry2.GetGesture(ShortcutAction.Find);
            var expected = DefaultShortcutBindings.Build()[ShortcutAction.Find];

            await Assert.That(loaded).IsNotNull();
            await Assert.That(loaded!.Key).IsEqualTo(expected.Key);
            await Assert.That(loaded.Modifiers).IsEqualTo(expected.Modifiers);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    /// <summary>
    ///     A persisted file holding only some actions is overlaid on top of the defaults so
    ///     unseeded actions still get their default gesture.
    /// </summary>
    [Test]
    public async Task LoadPartialFile_AfterStartup_AppliesDefaultsForMissingActions()
    {
        var path = CreateTempPath();

        try
        {
            var seedStore = new FileShortcutBindingsStore(path);
            var seedViewModel = new KeyboardShortcutsViewModel(new ShortcutRegistry(), seedStore, new LocalizationService(CultureInfo.InvariantCulture));
            seedViewModel.Rebind(ShortcutAction.Find, new KeyboardGesture { Key = "Q", Modifiers = KeyboardModifier.Alt });
            seedViewModel.SaveCommand.Execute(null);

            var freshStore = new FileShortcutBindingsStore(path);
            var freshRegistry = new ShortcutRegistry(freshStore.Load());

            await Assert.That(freshRegistry.GetGesture(ShortcutAction.Find)!.Key).IsEqualTo("Q");
            await Assert.That(freshRegistry.GetGesture(ShortcutAction.ClearTraffic)!.Key).IsEqualTo("K");
            await Assert.That(freshRegistry.GetGesture(ShortcutAction.ToggleCapture)!.Key).IsEqualTo("R");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string CreateTempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"proxyfan-shortcut-e2e-{Guid.NewGuid():N}.json");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
