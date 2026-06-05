using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PluginItemViewModel" />.
/// </summary>
[NotInParallel]
public sealed class PluginItemViewModelTests
{
    [Test]
    public async Task Constructor_LoadedPlugin_ShowsLoadedStatus()
    {
        var snapshot = new PluginStateSnapshot("p.id", "MyPlug", "1.0.0", "Author", "Desc", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: null);
        var store = new InMemoryStore();
        var opener = new RecordingOpener();

        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => { });

        await Assert.That(viewModel.Identifier).IsEqualTo("p.id");
        await Assert.That(viewModel.Name).IsEqualTo("MyPlug");
        await Assert.That(viewModel.Version).IsEqualTo("1.0.0");
        await Assert.That(viewModel.Author).IsEqualTo("Author");
        await Assert.That(viewModel.Description).IsEqualTo("Desc");
        await Assert.That(viewModel.ApiVersion).IsEqualTo("1.0");
        await Assert.That(viewModel.IsLoaded).IsTrue();
        await Assert.That(viewModel.ErrorMessage).IsNull();
        await Assert.That(viewModel.Status).IsEqualTo("Loaded");
        await Assert.That(viewModel.IsEnabled).IsTrue();
        await Assert.That(viewModel.IsFolderAvailable).IsFalse();
    }

    [Test]
    public async Task Constructor_FailedPlugin_ShowsFailedStatusWithError()
    {
        var snapshot = new PluginStateSnapshot("p.id", "Broken", "0.1", "Anon", "Bad", "1.0", isLoaded: false, errorMessage: "boom", sourceDirectory: null);
        var store = new InMemoryStore();
        var opener = new RecordingOpener();

        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => { });

        await Assert.That(viewModel.IsLoaded).IsFalse();
        await Assert.That(viewModel.ErrorMessage).IsEqualTo("boom");
        await Assert.That(viewModel.Status).IsEqualTo("Failed");
    }

    [Test]
    public async Task Constructor_DisabledIdentifier_StartsDisabled()
    {
        var snapshot = new PluginStateSnapshot("p.disabled", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: null);
        var store = new InMemoryStore();
        store.SetEnabled("p.disabled", isEnabled: false);
        var opener = new RecordingOpener();

        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => { });

        await Assert.That(viewModel.IsEnabled).IsFalse();
    }

    [Test]
    public async Task IsEnabled_Set_PersistsAndFiresCallback()
    {
        var snapshot = new PluginStateSnapshot("p.toggle", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: null);
        var store = new InMemoryStore();
        var opener = new RecordingOpener();
        var callbackCount = 0;
        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => callbackCount++);

        viewModel.IsEnabled = false;

        await Assert.That(viewModel.IsEnabled).IsFalse();
        await Assert.That(store.IsDisabled("p.toggle")).IsTrue();
        await Assert.That(callbackCount).IsEqualTo(1);
    }

    [Test]
    public async Task OpenFolderCommand_WithSourceDirectory_InvokesOpener()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-pivm-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var snapshot = new PluginStateSnapshot("p.open", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: directory);
            var store = new InMemoryStore();
            var opener = new RecordingOpener();
            var viewModel = new PluginItemViewModel(snapshot, store, opener, () => { });

            await Assert.That(viewModel.IsFolderAvailable).IsTrue();
            viewModel.OpenFolderCommand.Execute(null);

            await Assert.That(opener.LastOpened).IsEqualTo(directory);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (Exception ex) { _ = ex; }
        }
    }

    [Test]
    public async Task OpenFolderCommand_WithoutSourceDirectory_DoesNotInvoke()
    {
        var snapshot = new PluginStateSnapshot("p.nofolder", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: null);
        var store = new InMemoryStore();
        var opener = new RecordingOpener();
        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => { });

        viewModel.OpenFolderCommand.Execute(null);

        await Assert.That(opener.LastOpened).IsNull();
    }

    [Test]
    public async Task RemoveCommand_WithSourceDirectory_DisablesAndDeletes()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-pivm-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var snapshot = new PluginStateSnapshot("p.remove", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: directory);
            var store = new InMemoryStore();
            var opener = new RecordingOpener();
            var callbackCount = 0;
            var viewModel = new PluginItemViewModel(snapshot, store, opener, () => callbackCount++);

            viewModel.RemoveCommand.Execute(null);

            await Assert.That(store.IsDisabled("p.remove")).IsTrue();
            await Assert.That(Directory.Exists(directory)).IsFalse();
            await Assert.That(callbackCount).IsEqualTo(1);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (Exception ex) { _ = ex; }
        }
    }

    [Test]
    public async Task RemoveCommand_WithSourceDirectory_DeleteFails_DoesNotDisableOrNotify()
    {
        var directory = Path.Combine(Path.GetTempPath(), "proxyfan-pivm-" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var snapshot = new PluginStateSnapshot("p.removefail", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: directory);
            var store = new InMemoryStore();
            var opener = new RecordingOpener();
            var callbackCount = 0;
            var viewModel = new PluginItemViewModel(
                snapshot,
                store,
                opener,
                () => callbackCount++,
                (_, _) => throw new IOException("locked"));

            viewModel.RemoveCommand.Execute(null);

            await Assert.That(store.IsDisabled("p.removefail")).IsFalse();
            await Assert.That(callbackCount).IsEqualTo(0);
            await Assert.That(viewModel.ErrorMessage).IsNotNull();
            await Assert.That(viewModel.ErrorMessage).Contains("Failed to remove plugin folder:");
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (Exception ex) { _ = ex; }
        }
    }

    [Test]
    public async Task RemoveCommand_WithoutSourceDirectory_DisablesOnly()
    {
        var snapshot = new PluginStateSnapshot("p.removeonly", "P", "1", "A", "D", "1.0", isLoaded: true, errorMessage: null, sourceDirectory: null);
        var store = new InMemoryStore();
        var opener = new RecordingOpener();
        var callbackCount = 0;
        var viewModel = new PluginItemViewModel(snapshot, store, opener, () => callbackCount++);

        viewModel.RemoveCommand.Execute(null);

        await Assert.That(store.IsDisabled("p.removeonly")).IsTrue();
        await Assert.That(callbackCount).IsEqualTo(1);
    }

    private sealed class InMemoryStore : IPluginEnabledStateStore
    {
        private readonly HashSet<string> _disabled = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> GetDisabledIdentifiers()
        {
            return _disabled;
        }

        public bool IsDisabled(string identifier)
        {
            return _disabled.Contains(identifier);
        }

        public void SetEnabled(string identifier, bool isEnabled)
        {
            if (isEnabled)
            {
                _disabled.Remove(identifier);
            }
            else
            {
                _disabled.Add(identifier);
            }
        }
    }

    private sealed class RecordingOpener : IPluginFolderOpener
    {
        public string? LastOpened { get; private set; }

        public void Open(string directoryPath)
        {
            LastOpened = directoryPath;
        }
    }
}
