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

    /// <summary>
    ///     The OpenMapLocalCommand delegates to <see cref="StubToolWindowOpener.OpenMapLocal" />.
    /// </summary>
    [Test]
    public async Task OpenMapLocalCommand_WhenInvoked_OpensMapLocalWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenMapLocalCommand.Execute(null);

        await Assert.That(opener.OpenMapLocalCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenMapRemoteCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenMapRemoteCommand delegates to <see cref="StubToolWindowOpener.OpenMapRemote" />.
    /// </summary>
    [Test]
    public async Task OpenMapRemoteCommand_WhenInvoked_OpensMapRemoteWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenMapRemoteCommand.Execute(null);

        await Assert.That(opener.OpenMapRemoteCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenMapLocalCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenThrottleCommand delegates to <see cref="StubToolWindowOpener.OpenThrottle" />.
    /// </summary>
    [Test]
    public async Task OpenThrottleCommand_WhenInvoked_OpensThrottleWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenThrottleCommand.Execute(null);

        await Assert.That(opener.OpenThrottleCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenMapRemoteCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenThemeCommand delegates to <see cref="StubToolWindowOpener.OpenTheme" />.
    /// </summary>
    [Test]
    public async Task OpenThemeCommand_WhenInvoked_OpensThemeWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenThemeCommand.Execute(null);

        await Assert.That(opener.OpenThemeCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenThrottleCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenCertificateManagerCommand delegates to <see cref="StubToolWindowOpener.OpenCertificateManager" />.
    /// </summary>
    [Test]
    public async Task OpenCertificateManagerCommand_WhenInvoked_OpensCertificateManagerWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenCertificateManagerCommand.Execute(null);

        await Assert.That(opener.OpenCertificateManagerCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenThemeCallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     The OpenSecureSocketsLayerProxyingCommand delegates to <see cref="StubToolWindowOpener.OpenSecureSocketsLayerProxying" />.
    /// </summary>
    [Test]
    public async Task OpenSecureSocketsLayerProxyingCommand_WhenInvoked_OpensSecureSocketsLayerProxyingWindow()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            port: 8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenSecureSocketsLayerProxyingCommand.Execute(null);

        await Assert.That(opener.OpenSecureSocketsLayerProxyingCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenThemeCallCount).IsEqualTo(0);
    }
}
