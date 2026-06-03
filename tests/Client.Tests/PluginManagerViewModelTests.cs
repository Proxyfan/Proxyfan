using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using Proxyfan.Framework.Extensibility;
using Proxyfan.Plugin.Abstractions;
using Proxyfan.Presentation.Threading;
using System;
using System.Collections.Generic;
using System.IO;
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
        harness.Registry.TryInitialize(new StubPlugin("com.example.test", "Test", "1.0.0", "Author", "Description", "1.0.0"), harness.Host, null);

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
        harness.Registry.TryInitialize(new StubPlugin("com.example.bad", "Bad", "1.0.0", "Author", "Description", "2.0.0"), harness.Host, null);

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

        harness.Registry.TryInitialize(new StubPlugin("com.example.late", "Late", "1.0.0", "Author", "Description", "1.0.0"), harness.Host, null);
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
        harness.Registry.TryInitialize(new StubPlugin("com.example.toggle", "T", "1.0", "A", "D", "1.0.0"), harness.Host, null);
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
        harness.Registry.TryInitialize(new StubPlugin("com.example.remove", "R", "1.0", "A", "D", "1.0.0"), harness.Host, null);
        var viewModel = harness.CreateViewModel();

        viewModel.Plugins[0].RemoveCommand.Execute(null);

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(harness.Store.IsDisabled("com.example.remove")).IsTrue();
    }

    [Test]
    public async Task CheckForUpdates_NullManifest_FlagsFailure()
    {
        using var harness = new ActivationHarness();
        harness.UpdateFeed.NextManifest = null;
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
        harness.Registry.TryInitialize(new StubPlugin("com.x", "X", "1.0.0", "A", "D", "1.0.0"), harness.Host, null);
        var entry = new PluginUpdateEntry
        {
            Identifier = "com.x",
            LatestVersion = "1.0.0",
            DownloadUrl = "https://example.com/x.zip",
            MinimumApiVersion = "1.0",
        };
        harness.UpdateFeed.NextManifest = new PluginUpdateManifest { Plugins = [entry] };
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
        harness.Registry.TryInitialize(new StubPlugin("com.x", "X", "1.0.0", "A", "D", "1.0.0"), harness.Host, null);
        var entry = new PluginUpdateEntry
        {
            Identifier = "com.x",
            LatestVersion = "1.1.0",
            DownloadUrl = "https://example.com/x.zip",
            MinimumApiVersion = "1.0",
        };
        harness.UpdateFeed.NextManifest = new PluginUpdateManifest { Plugins = [entry] };
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

        harness.DirectoryWatcher.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("Plugins folder changed");
    }

    [Test]
    public async Task DirectoryWatcher_FireChange_DisposeBeforeScheduledUpdate_DoesNotMutateState()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        using var harness = new ActivationHarness(scheduler);
        var viewModel = harness.CreateViewModel();

        harness.DirectoryWatcher.FireChange();
        await Assert.That(viewModel.IsRestartRequired).IsFalse();

        viewModel.Dispose();
        scheduler.RunNext();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
        await Assert.That(viewModel.UpdateCheckStatus).IsEmpty();
    }

    [Test]
    public async Task DirectoryWatcher_FireChange_SchedulesUiUpdate()
    {
        var scheduler = new DeferredUserInterfaceScheduler();
        using var harness = new ActivationHarness(scheduler);
        var viewModel = harness.CreateViewModel();

        harness.DirectoryWatcher.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
        scheduler.RunNext();

        await Assert.That(viewModel.IsRestartRequired).IsTrue();
        await Assert.That(viewModel.UpdateCheckStatus).Contains("Plugins folder changed");
    }

    [Test]
    public async Task Construct_OnCreation_StartsDirectoryWatcher()
    {
        using var harness = new ActivationHarness();

        var viewModel = harness.CreateViewModel();

        await Assert.That(harness.DirectoryWatcher.IsStarted).IsTrue();
        viewModel.Dispose();
    }

    [Test]
    public async Task Dispose_AfterCreation_UnsubscribesFromWatcher()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();
        viewModel.Dispose();

        harness.DirectoryWatcher.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
    }

    [Test]
    public async Task Dispose_CalledTwice_NoThrow()
    {
        using var harness = new ActivationHarness();
        var viewModel = harness.CreateViewModel();

        viewModel.Dispose();
        viewModel.Dispose();
        harness.DirectoryWatcher.FireChange();

        await Assert.That(viewModel.IsRestartRequired).IsFalse();
    }

    private sealed class ActivationHarness : IDisposable
    {
        private readonly string _rootDirectory;

        public PluginActivationService ActivationService { get; }

        public StubDirectoryWatcher DirectoryWatcher { get; }

        public RecordingPluginHost Host { get; }

        public RecordingOpener Opener { get; }

        public PluginRegistry Registry { get; }

        public InMemoryStore Store { get; }

        public IUserInterfaceScheduler UserInterfaceScheduler { get; }

        public StubUpdateFeed UpdateFeed { get; }

        public ActivationHarness(IUserInterfaceScheduler? userInterfaceScheduler = null)
        {
            Registry = new PluginRegistry();
            Host = new RecordingPluginHost("1.0.0");
            Store = new InMemoryStore();
            Opener = new RecordingOpener();
            UpdateFeed = new StubUpdateFeed();
            DirectoryWatcher = new StubDirectoryWatcher();
            UserInterfaceScheduler = userInterfaceScheduler ?? InlineUserInterfaceScheduler.Instance;
            _rootDirectory = Path.Combine(Path.GetTempPath(), "proxyfan-pmvm-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_rootDirectory);
            var rootProvider = new PluginRootDirectoryProvider(_rootDirectory);
            var scanner = new PluginDirectoryScanner();
            var factory = new NeverInstantiateFactory();
            var loader = new PluginLoader(scanner, factory, Registry, Store);
            ActivationService = new PluginActivationService(loader, Host, rootProvider, Registry);
        }

        public PluginManagerViewModel CreateViewModel()
        {
            var viewModel = new PluginManagerViewModel(Registry, Store, Opener, ActivationService, UpdateFeed, Host, DirectoryWatcher, UserInterfaceScheduler);
            return viewModel;
        }

        public void Dispose()
        {
            try { Directory.Delete(_rootDirectory, recursive: true); } catch (Exception ex) { _ = ex; }
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

    private sealed class NeverInstantiateFactory : IPluginInstanceFactory
    {
        public PluginInstantiationResult Create(PluginCandidate candidate)
        {
            return new PluginInstantiationResult(plugin: null, loadContext: null, errorMessage: "Stub factory cannot instantiate plugins in tests.", isSuccess: false);
        }
    }

    private sealed class RecordingOpener : IPluginFolderOpener
    {
        public void Open(string directoryPath)
        {
        }
    }

    private sealed class StubPlugin : IProxyfanPlugin
    {
        public StubPlugin(string id, string name, string version, string author, string description, string apiVersion)
        {
            Metadata = new PluginMetadata(id, name, version, author, description, apiVersion);
        }

        public PluginMetadata Metadata { get; }

        public void Initialize(IPluginHost host)
        {
        }
    }

    private sealed class StubUpdateFeed : IPluginUpdateFeed
    {
        public PluginUpdateManifest? NextManifest { get; set; }

        public Task<PluginUpdateManifest?> FetchAsync(System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(NextManifest);
        }
    }

    private sealed class StubDirectoryWatcher : IPluginDirectoryWatcher
    {
        public event PluginsDirectoryChangedHandler? PluginsDirectoryChanged;

        public bool IsStarted { get; private set; }

        public void Dispose()
        {
            IsStarted = false;
        }

        public void FireChange()
        {
            PluginsDirectoryChanged?.Invoke();
        }

        public void Start()
        {
            IsStarted = true;
        }
    }

    private sealed class DeferredUserInterfaceScheduler : IUserInterfaceScheduler
    {
        private readonly Queue<UserInterfaceWorkItem> _workItems = [];

        public bool HasAccess()
        {
            return false;
        }

        public void Post(UserInterfaceWorkItem action)
        {
            _workItems.Enqueue(action);
        }

        public void RunNext()
        {
            if (_workItems.Count == 0)
            {
                return;
            }

            var action = _workItems.Dequeue();
            action();
        }
    }
}
