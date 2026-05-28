using Proxyfan.Client.Tools.ViewModels;
using Proxyfan.Framework.Extensibility;
using Proxyfan.Plugin.Abstractions;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="PluginItemViewModel" />.
/// </summary>
public sealed class PluginItemViewModelTests
{
    [Test]
    public async Task Constructor_LoadedPlugin_ShowsLoadedStatus()
    {
        var metadata = new PluginMetadata("p.id", "MyPlug", "1.0.0", "Author", "Desc", "1.0");
        var loaded = new LoadedPlugin(metadata, null, true, null);

        var viewModel = new PluginItemViewModel(loaded);

        await Assert.That(viewModel.Identifier).IsEqualTo("p.id");
        await Assert.That(viewModel.Name).IsEqualTo("MyPlug");
        await Assert.That(viewModel.Version).IsEqualTo("1.0.0");
        await Assert.That(viewModel.Author).IsEqualTo("Author");
        await Assert.That(viewModel.Description).IsEqualTo("Desc");
        await Assert.That(viewModel.ApiVersion).IsEqualTo("1.0");
        await Assert.That(viewModel.IsLoaded).IsTrue();
        await Assert.That(viewModel.ErrorMessage).IsNull();
        await Assert.That(viewModel.Status).IsEqualTo("Loaded");
    }

    [Test]
    public async Task Constructor_FailedPlugin_ShowsFailedStatusWithError()
    {
        var metadata = new PluginMetadata("p.id", "Broken", "0.1", "Anon", "Bad", "1.0");
        var failed = new LoadedPlugin(metadata, null, false, "boom");

        var viewModel = new PluginItemViewModel(failed);

        await Assert.That(viewModel.IsLoaded).IsFalse();
        await Assert.That(viewModel.ErrorMessage).IsEqualTo("boom");
        await Assert.That(viewModel.Status).IsEqualTo("Failed");
    }
}
