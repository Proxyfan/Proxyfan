using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Framework.Extensibility;
using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PluginManagerViewModel" />.
/// </summary>
public sealed class PluginManagerViewModelTests
{
    [Test]
    public async Task Construct_EmptyRegistry_ProducesEmptyList()
    {
        var registry = new PluginRegistry();

        var viewModel = new PluginManagerViewModel(registry);

        await Assert.That(viewModel.Plugins).IsEmpty();
        await Assert.That(viewModel.Summary).Contains("0 plugin");
    }

    [Test]
    public async Task Construct_RegistryWithLoadedPlugin_ExposesItemViewModel()
    {
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0.0");
        registry.TryInitialize(new StubPlugin("com.example.test", "Test", "1.0.0", "Author", "Description", "1.0.0"), host);

        var viewModel = new PluginManagerViewModel(registry);

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
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0.0");
        registry.TryInitialize(new StubPlugin("com.example.bad", "Bad", "1.0.0", "Author", "Description", "2.0.0"), host);

        var viewModel = new PluginManagerViewModel(registry);

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
        var registry = new PluginRegistry();
        var host = new RecordingPluginHost("1.0.0");
        var viewModel = new PluginManagerViewModel(registry);
        await Assert.That(viewModel.Plugins).IsEmpty();

        registry.TryInitialize(new StubPlugin("com.example.late", "Late", "1.0.0", "Author", "Description", "1.0.0"), host);
        viewModel.RefreshCommand.Execute(null);

        await Assert.That(viewModel.Plugins).Count().IsEqualTo(1);
        await Assert.That(viewModel.Plugins[0].Name).IsEqualTo("Late");
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
}
