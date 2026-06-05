using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Plugin.Abstractions;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PluginManagerViewModel" />.
/// </summary>
[NotInParallel]
public sealed class PluginManagerViewModelTests
{
    [Test]
    public async Task Construct_EmptyRegistry_ProducesEmptyList()
    {
        using var harness = new ActivationHarness();

        var viewModel = harness.CreateViewModel();

        await Assert.That(viewModel.Plugins).IsEmpty();
        await Assert.That(viewModel.Summary).Contains("0 plugin");
        await Assert.That(viewModel.IsRestartRequired).IsFalse();
    }

    [Test]
    public async Task Construct_RegistryWithLoadedPlugin_ExposesItemViewModel()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.AddPlugin(new StubPluginLoadState("com.example.test", "Test", "1.0.0", "Author", "Description", "1.0.0", isLoaded: true));

        var viewModel = harness.CreateViewModel();

        await Assert.That(viewModel.Plugins).Count().IsEqualTo(1);
        var item = viewModel.Plugins[0];
        await Assert.That(item.Name).IsEqualTo("Test");
        await Assert.That(item.IsLoaded).IsTrue();
        await Assert.That(item.Status).IsEqualTo("Loaded");
        await Assert.That(item.ErrorMessage).IsNull();
        await Assert.That(viewModel.Summary).Contains("1 loaded");
    }

    [Test]
    public async Task Construct_RegistryWithIncompatiblePlugin_ExposesFailedItem()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.AddPlugin(new StubPluginLoadState("com.example.bad", "Bad", "1.0.0", "Author", "Description", "2.0.0", isLoaded: false, errorMessage: "Incompatible API version.", sourceDirectory: null));

        var viewModel = harness.CreateViewModel();

        await Assert.That(viewModel.Plugins).Count().IsEqualTo(1);
        var item = viewModel.Plugins[0];
        await Assert.That(item.IsLoaded).IsFalse();
        await Assert.That(item.Status).IsEqualTo("Failed");
        await Assert.That(item.ErrorMessage).IsNotNull();
        await Assert.That(viewModel.Summary).Contains("1 failed");
    }

    [Test]
    public async Task RefreshCommand_AfterPluginAdded_PicksUpNewEntry()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();
        await Assert.That(viewModel.Plugins).IsEmpty();

        harness.Coordinator.AddPlugin(new StubPluginLoadState("com.example.late", "Late", "1.0.0", "Author", "Description", "1.0.0", isLoaded: true));
        viewModel.RefreshCommand.Execute(null);

        await Assert.That(viewModel.Plugins).Count().IsEqualTo(1);
        await Assert.That(viewModel.Plugins[0].Name).IsEqualTo("Late");
    }

    [Test]
    public async Task ReloadCommand_AfterInvoke_FlagsRestartRequired()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();

        viewModel.ReloadCommand.Execute(null);

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
    }

    [Test]
    public async Task PluginToggle_WhenDisabling_FlagsRestartRequired()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.AddPlugin(new StubPluginLoadState("com.example.toggle", "T", "1.0", "A", "D", "1.0.0", isLoaded: true));
        var viewModel = harness.CreateViewModel();
        await Assert.That(viewModel.IsRestartRequired).IsFalse();

        viewModel.Plugins[0].IsEnabled = false;

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(harness.Store.IsDisabled("com.example.toggle")).IsTrue();
    }

    [Test]
    public async Task PluginRemove_WhenInvoked_FlagsRestartRequired()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.AddPlugin(new StubPluginLoadState("com.example.remove", "R", "1.0", "A", "D", "1.0.0", isLoaded: true));
        var viewModel = harness.CreateViewModel();

        viewModel.Plugins[0].RemoveCommand.Execute(null);

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(harness.Store.IsDisabled("com.example.remove")).IsTrue();
    }

    [Test]
    public async Task CheckForUpdates_NullManifest_FlagsFailure()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.SetUpdateResult(null);
        var viewModel = harness.CreateViewModel();

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        await Assert.That(viewModel.IsUpdateCheckFailed).IsTrue();
        await Assert.That(viewModel.IsCheckingForUpdates).IsFalse();
        await Assert.That(viewModel.AvailableUpdates).IsEmpty();
        await Assert.That(viewModel.IsAnyUpdateAvailable).IsFalse();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("failed");
    }

    [Test]
    public async Task CheckForUpdates_NoUpgradeAvailable_ClearsList()
    {
        using var harness = new ActivationHarness();
        harness.Coordinator.SetUpdateResult([]);
        var viewModel = harness.CreateViewModel();

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        await Assert.That(viewModel.IsUpdateCheckFailed).IsFalse();
        await Assert.That(viewModel.AvailableUpdates).IsEmpty();
        await Assert.That(viewModel.IsAnyUpdateAvailable).IsFalse();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("up to date");
    }

    [Test]
    public async Task CheckForUpdates_NewerAvailable_PopulatesAndFlagsAvailable()
    {
        using var harness = new ActivationHarness();
        var update = new PluginUpdateAvailability
        {
            Identifier = "com.x",
            Name = "X",
            Author = "A",
            CurrentVersion = "1.0.0",
            LatestVersion = "1.1.0",
            DownloadUrl = "https://example.com/x.zip",
            IsCompatible = true,
        };
        harness.Coordinator.SetUpdateResult([update]);
        var viewModel = harness.CreateViewModel();

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        await Assert.That(viewModel.AvailableUpdates).Count().IsEqualTo(1);
        await Assert.That(viewModel.AvailableUpdates[0].LatestVersion).IsEqualTo("1.1.0");
        await Assert.That(viewModel.IsAnyUpdateAvailable).IsTrue();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("update");
    }

    [Test]
    public async Task DirectoryWatcher_FireChange_FlagsRestartRequired()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();
        await Assert.That(viewModel.IsRestartRequired).IsFalse();

        harness.Coordinator.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("Plugins folder changed");
    }

    [Test]
    public async Task DirectoryWatcher_FireChange_PostsViaUserInterfaceScheduler()
    {
        var scheduler = new RecordingUserInterfaceScheduler(runImmediately: true);
        using var harness = new ActivationHarness(scheduler);
        var viewModel = harness.CreateViewModel();

        harness.Coordinator.FireChange();

        await Assert.That(scheduler.PostCallCount).IsEqualTo(1);
        await Assert.That(viewModel.IsRestartRequired).IsTrue();
    }

    [Test]
    public async Task DirectoryWatcher_FireChange_DisposedBeforePostedAction_NoUpdate()
    {
        var scheduler = new RecordingUserInterfaceScheduler(runImmediately: false);
        using var harness = new ActivationHarness(scheduler);
        var viewModel = harness.CreateViewModel();

        harness.Coordinator.FireChange();
        viewModel.Dispose();
        scheduler.RunPending();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
        await Assert.That(viewModel.UpdateCheckStatus).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Construct_OnCreation_StartsDirectoryWatcher()
    {
        using var harness = new ActivationHarness();

        var viewModel = harness.CreateViewModel();

        await Assert.That(harness.Coordinator.IsStarted).IsTrue();
        viewModel.Dispose();
    }

    [Test]
    public async Task Dispose_AfterCreation_UnsubscribesFromWatcher()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();
        viewModel.Dispose();

        harness.Coordinator.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
    }

    [Test]
    public async Task Dispose_CalledTwice_NoThrow()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();

        viewModel.Dispose();
        viewModel.Dispose();
        harness.Coordinator.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
    }

    private sealed class ActivationHarness : IDisposable
    {
        public StubPluginManagerCoordinator Coordinator { get; }

        public RecordingOpener Opener { get; }

        public InMemoryStore Store { get; }

        public IUserInterfaceScheduler UserInterfaceScheduler { get; }

        public ActivationHarness(IUserInterfaceScheduler? userInterfaceScheduler = null)
        {
            Coordinator = new StubPluginManagerCoordinator();
            Store = new InMemoryStore();
            Opener = new RecordingOpener();
            UserInterfaceScheduler = userInterfaceScheduler ?? InlineUserInterfaceScheduler.Instance;
        }

        public PluginManagerViewModel CreateViewModel()
        {
            var viewModel = new PluginManagerViewModel(Coordinator, Store, Opener, UserInterfaceScheduler);
            return viewModel;
        }

        public void Dispose()
        {
        }
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
        public void Open(string directoryPath)
        {
        }
    }

    private sealed class RecordingUserInterfaceScheduler : IUserInterfaceScheduler
    {
        private readonly bool _runImmediately;
        private UserInterfaceWorkItem? _pending;

        public RecordingUserInterfaceScheduler(bool runImmediately)
        {
            _runImmediately = runImmediately;
        }

        public int PostCallCount { get; private set; }

        public bool HasAccess()
        {
            return false;
        }

        public void Post(UserInterfaceWorkItem action)
        {
            PostCallCount++;
            if (_runImmediately)
            {
                action();
                return;
            }

            _pending = action;
        }

        public void RunPending()
        {
            if (_pending is null)
            {
                return;
            }

            var action = _pending;
            _pending = null;
            action();
        }
    }

    private sealed class StubPluginLoadState : IPluginLoadState
    {
        public StubPluginLoadState(string id, string name, string version, string author, string description, string apiVersion, bool isLoaded)
            : this(id, name, version, author, description, apiVersion, isLoaded, errorMessage: null, sourceDirectory: null)
        {
        }

        public StubPluginLoadState(string id, string name, string version, string author, string description, string apiVersion, bool isLoaded, string? errorMessage, string? sourceDirectory)
        {
            Metadata = new PluginMetadata(id, name, version, author, description, apiVersion);
            IsLoaded = isLoaded;
            ErrorMessage = errorMessage;
            SourceDirectory = sourceDirectory;
        }

        public string? ErrorMessage { get; }

        public bool IsLoaded { get; }

        public PluginMetadata Metadata { get; }

        public string? SourceDirectory { get; }
    }

    private sealed class StubPluginManagerCoordinator : IPluginManagerCoordinator
    {
        private readonly List<IPluginLoadState> _plugins = [];
        private IReadOnlyList<PluginUpdateAvailability>? _updateResult = [];

        public event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

        public bool IsStarted { get; private set; }

        public IReadOnlyList<IPluginLoadState> Plugins => _plugins;

        public void AddPlugin(IPluginLoadState plugin) => _plugins.Add(plugin);

        public void Dispose()
        {
            IsStarted = false;
        }

        public Task<IReadOnlyList<PluginUpdateAvailability>?> FetchUpdatesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_updateResult);
        }

        public void FireChange() => PluginsDirectoryChanged?.Invoke();

        public void Reload()
        {
        }

        public void SetUpdateResult(IReadOnlyList<PluginUpdateAvailability>? result) => _updateResult = result;

        public void Start() => IsStarted = true;
    }
}
