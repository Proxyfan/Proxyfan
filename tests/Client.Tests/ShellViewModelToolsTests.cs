using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for the Tools menu commands on <see cref="ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelToolsTests
{
    /// <summary>
    ///     The OpenBlockListCommand delegates to <see cref="StubToolWindowOpener.OpenBlockList" />.
    /// </summary>
    [Test]
    public async Task OpenBlockListCommand_WhenInvoked_OpensBlockListWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenBlockListCommand.Execute(null);

        await Assert.That(opener.OpenBlockListCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenAllowListCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenAllowListCommand delegates to <see cref="StubToolWindowOpener.OpenAllowList" />.
    /// </summary>
    [Test]
    public async Task OpenAllowListCommand_WhenInvoked_OpensAllowListWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenAllowListCommand.Execute(null);

        await Assert.That(opener.OpenAllowListCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenBlockListCallCount).IsEqualTo(0);
    }
}
