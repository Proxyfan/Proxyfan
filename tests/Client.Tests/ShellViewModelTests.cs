using Proxyfan.Client.Shell.ViewModels;
using Proxyfan.Client.Tests.Stubs;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Tests for <see cref="ShellViewModel" />.
/// </summary>
public sealed class ShellViewModelTests
{
    private static ShellViewModel CreateViewModel(StubSystemProxy systemProxy, int port)
    {
        return ShellViewModelFactory.Create(systemProxy, port);
    }

    /// <summary>
    ///     Verifies that a new view model starts with system proxy disabled.
    /// </summary>
    [Test]
    public async Task IsSystemProxyEnabled_InitialState_IsFalse()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that toggling with proxy disabled calls RegisterAsync with the configured port.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_WhenDisabled_RegistersProxy()
    {
        var systemProxy = new StubSystemProxy();
        var viewModel = CreateViewModel(systemProxy, 8888);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

        await Assert.That(systemProxy.RegisteredPorts.Count).IsEqualTo(1);
        await Assert.That(systemProxy.RegisteredPorts[0]).IsEqualTo(8888);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that toggling when proxy is enabled calls UnregisterAsync.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_WhenEnabled_UnregistersProxy()
    {
        var systemProxy = new StubSystemProxy();
        var viewModel = CreateViewModel(systemProxy, 8080);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);

        await Assert.That(systemProxy.UnregisterCount).IsEqualTo(1);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();
    }

    /// <summary>
    ///     Verifies that multiple toggles alternate between enabled and disabled states.
    /// </summary>
    [Test]
    public async Task ToggleSystemProxyCommand_MultipleToggles_AlternatesState()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsFalse();

        await viewModel.ToggleSystemProxyCommand.ExecuteAsync(null);
        await Assert.That(viewModel.IsSystemProxyEnabled).IsTrue();
    }

    /// <summary>
    ///     Verifies that the TrafficList property is exposed and not null.
    /// </summary>
    [Test]
    public async Task TrafficList_AfterConstruction_IsExposed()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        await Assert.That(viewModel.TrafficList).IsNotNull();
        await Assert.That(viewModel.TrafficList.IsCapturing).IsTrue();
    }

    /// <summary>
    ///     Verifies that every Open* command delegates to the corresponding opener method exactly once.
    /// </summary>
    [Test]
    public async Task OpenCommands_AllInvoked_DelegateToToolWindowOpener()
    {
        var opener = new StubToolWindowOpener();
        var viewModel = ShellViewModelFactory.Create(
            new StubSystemProxy(),
            8080,
            new ShellViewModelFactory.StubFilePickerService(),
            new ShellViewModelFactory.StubHarExporter(),
            new ShellViewModelFactory.StubHarImporter(),
            opener);

        viewModel.OpenAllowListCommand.Execute(null);
        viewModel.OpenBlockListCommand.Execute(null);
        viewModel.OpenBreakpointCommand.Execute(null);
        viewModel.OpenCertificateManagerCommand.Execute(null);
        viewModel.OpenComposerCommand.Execute(null);
        viewModel.OpenCustomColumnsCommand.Execute(null);
        viewModel.OpenDiffToolCommand.Execute(null);
        viewModel.OpenDomainNameSystemSpoofingCommand.Execute(null);
        viewModel.OpenMapLocalCommand.Execute(null);
        viewModel.OpenMapRemoteCommand.Execute(null);
        viewModel.OpenPluginManagerCommand.Execute(null);
        viewModel.OpenPreferencesCommand.Execute(null);
        viewModel.OpenRemoteDevicesCommand.Execute(null);
        viewModel.OpenReverseProxyCommand.Execute(null);
        viewModel.OpenScriptingCommand.Execute(null);
        viewModel.OpenSecureSocketsLayerProxyingCommand.Execute(null);
        viewModel.OpenThemeCommand.Execute(null);
        viewModel.OpenThrottleCommand.Execute(null);

        await Assert.That(opener.OpenAllowListCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenBlockListCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenBreakpointCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenCertificateManagerCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenComposerCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenCustomColumnsCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenDiffToolCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenDomainNameSystemSpoofingCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenMapLocalCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenMapRemoteCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenPluginManagerCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenPreferencesCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenRemoteDevicesCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenReverseProxyCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenScriptingCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenSecureSocketsLayerProxyingCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenThemeCallCount).IsEqualTo(1);
        await Assert.That(opener.OpenThrottleCallCount).IsEqualTo(1);
    }

    /// <summary>
    ///     Verifies that ExitCommand is safe to invoke even without an Avalonia desktop lifetime present.
    /// </summary>
    [Test]
    public async Task ExitCommand_NoDesktopLifetime_DoesNotThrow()
    {
        var viewModel = CreateViewModel(new StubSystemProxy(), 8080);

        viewModel.ExitCommand.Execute(null);

        await Assert.That(viewModel).IsNotNull();
    }

    /// <summary>
    ///     Verifies that SaveSessionAsync is a no-op when the file picker returns null (user cancels).
    /// </summary>
    [Test]
    public async Task SaveSessionCommand_PickerReturnsNull_DoesNotExport()
    {
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = null };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, new ShellViewModelFactory.StubHarImporter());

        await viewModel.SaveSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForWriteCallCount).IsEqualTo(1);
        await Assert.That(exporter.CallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that SaveSessionAsync writes a snapshot to the picked stream when provided.
    /// </summary>
    [Test]
    public async Task SaveSessionCommand_PickerReturnsStream_ExportsFlows()
    {
        using var memory = new System.IO.MemoryStream();
        var picker = new ShellViewModelFactory.StubFilePickerService { WriteStream = memory };
        var exporter = new ShellViewModelFactory.StubHarExporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, exporter, new ShellViewModelFactory.StubHarImporter());

        await viewModel.SaveSessionCommand.ExecuteAsync(null);

        await Assert.That(exporter.CallCount).IsEqualTo(1);
        await Assert.That(exporter.LastFlows).IsNotNull();
        await Assert.That(exporter.LastStream).IsSameReferenceAs(memory);
    }

    /// <summary>
    ///     Verifies that OpenSessionAsync is a no-op when the file picker returns null (user cancels).
    /// </summary>
    [Test]
    public async Task OpenSessionCommand_PickerReturnsNull_DoesNotImport()
    {
        var picker = new ShellViewModelFactory.StubFilePickerService { ReadStream = null };
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, new ShellViewModelFactory.StubHarExporter(), importer);

        await viewModel.OpenSessionCommand.ExecuteAsync(null);

        await Assert.That(picker.OpenForReadCallCount).IsEqualTo(1);
        await Assert.That(importer.CallCount).IsEqualTo(0);
    }

    /// <summary>
    ///     Verifies that OpenSessionAsync imports flows and loads them into the traffic list.
    /// </summary>
    [Test]
    public async Task OpenSessionCommand_PickerReturnsStream_ImportsFlowsAndLoadsThem()
    {
        using var memory = new System.IO.MemoryStream();
        var picker = new ShellViewModelFactory.StubFilePickerService { ReadStream = memory };
        var importer = new ShellViewModelFactory.StubHarImporter();
        var viewModel = ShellViewModelFactory.Create(new StubSystemProxy(), 8080, picker, new ShellViewModelFactory.StubHarExporter(), importer);

        await viewModel.OpenSessionCommand.ExecuteAsync(null);

        await Assert.That(importer.CallCount).IsEqualTo(1);
    }
}