using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Domain.Traffic.Tabs;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="TabViewModel" />.
/// </summary>
public sealed class TabViewModelTests
{
    [Test]
    public async Task Constructor_FromDomainTab_CopiesNameAndDefaultsCloseable()
    {
        var source = new TrafficWorkspaceTab("My Tab");

        var viewModel = new TabViewModel(source);

        await Assert.That(viewModel.Source).IsSameReferenceAs(source);
        await Assert.That(viewModel.Name).IsEqualTo("My Tab");
        await Assert.That(viewModel.Id).IsEqualTo(source.Id);
        await Assert.That(viewModel.IsCloseable).IsTrue();
    }

    [Test]
    public async Task Rename_WithValidName_UpdatesNameOnSource()
    {
        var source = new TrafficWorkspaceTab("Original");
        var viewModel = new TabViewModel(source);

        viewModel.Rename("Renamed");

        await Assert.That(source.Name).IsEqualTo("Renamed");
        await Assert.That(viewModel.Name).IsEqualTo("Renamed");
    }

    [Test]
    public async Task Rename_WithNull_IsIgnored()
    {
        var source = new TrafficWorkspaceTab("Original");
        var viewModel = new TabViewModel(source);

        viewModel.Rename(null);

        await Assert.That(viewModel.Name).IsEqualTo("Original");
    }

    [Test]
    public async Task Rename_WithWhitespace_IsIgnored()
    {
        var source = new TrafficWorkspaceTab("Original");
        var viewModel = new TabViewModel(source);

        viewModel.Rename("   ");

        await Assert.That(viewModel.Name).IsEqualTo("Original");
    }

    [Test]
    public async Task SourceChanged_OnDomainRename_PropagatesToViewModelName()
    {
        var source = new TrafficWorkspaceTab("Original");
        var viewModel = new TabViewModel(source);

        source.SetName("Externally Renamed");

        await Assert.That(viewModel.Name).IsEqualTo("Externally Renamed");
    }
}
